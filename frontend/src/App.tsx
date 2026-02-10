import type { FormEvent } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import Dashboard from "./Dashboard";

type Transaction = {
  id: number;
  date: string;
  rawDescription: string;
  merchant: string;
  amount: number;
  currency: string;
  balance: number | null;
};

type ImportBatch = {
  id: number;
  uploadedAt: string;
  extractedAtUtc: string | null;
  originalFileName: string;
  statementMonth: string;
  status: string;
  storageKey: string;
  sha256Hash: string | null;
  parserKey?: string | null;
};

type StagedTransaction = {
  id: number;
  importBatchId: number;
  rowIndex: number;
  bookingDate: string;
  valueDate: string | null;
  rawDescription: string;
  amount: number;
  currency: string | null;
  runningBalance: number | null;
  isApproved: boolean;
};

type CommitImportResult = {
  importBatchId: number;
  approvedCount: number;
  committedCount: number;
  skippedCount: number;
  status: string;
};

type Category = {
  id: number;
  name: string;
};

type MerchantRule = {
  id: number;
  pattern: string;
  matchType: string;
  normalizedMerchant: string;
  categoryId: number | null;
  categoryName: string | null;
  priority: number;
  createdAtUtc: string;
};

type RuleDraft = {
  pattern: string;
  normalizedMerchant: string;
  categoryId: string;
  priority: string;
};

type Route = "dashboard" | "imports" | "rules";

const App = () => {
  const getRouteFromPath = useCallback((): Route => {
    const path = window.location.pathname.toLowerCase();
    if (path.startsWith("/rules")) {
      return "rules";
    }
    if (path.startsWith("/imports")) {
      return "imports";
    }
    return "dashboard";
  }, []);

  const [route, setRoute] = useState<Route>(() => getRouteFromPath());
  const isDashboardRoute = route === "dashboard";
  const [importView, setImportView] = useState<"list" | "review">("list");
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [imports, setImports] = useState<ImportBatch[]>([]);
  const [importsLoading, setImportsLoading] = useState(true);
  const [importsError, setImportsError] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [statementMonth, setStatementMonth] = useState("");
  const [selectedImportId, setSelectedImportId] = useState<number | null>(null);
  const [extractedText, setExtractedText] = useState<string | null>(null);
  const [extractedTextLoading, setExtractedTextLoading] = useState(false);
  const [extractedTextError, setExtractedTextError] = useState<string | null>(
    null,
  );
  const [isExtracting, setIsExtracting] = useState(false);
  const [stagedRows, setStagedRows] = useState<StagedTransaction[]>([]);
  const [stagedLoading, setStagedLoading] = useState(false);
  const [stagedError, setStagedError] = useState<string | null>(null);
  const [isParsing, setIsParsing] = useState(false);
  const [isCommitting, setIsCommitting] = useState(false);
  const [commitMessage, setCommitMessage] = useState<string | null>(null);
  const [commitError, setCommitError] = useState<string | null>(null);
  const [rulesMonth, setRulesMonth] = useState(() => {
    const saved = localStorage.getItem("rulesMonth");
    if (saved) {
      return saved;
    }
    return new Date().toISOString().slice(0, 7);
  });
  const [unmappedMerchants, setUnmappedMerchants] = useState<string[]>([]);
  const [unmappedLoading, setUnmappedLoading] = useState(false);
  const [unmappedError, setUnmappedError] = useState<string | null>(null);
  const [merchantRules, setMerchantRules] = useState<MerchantRule[]>([]);
  const [rulesLoading, setRulesLoading] = useState(false);
  const [rulesError, setRulesError] = useState<string | null>(null);
  const [categories, setCategories] = useState<Category[]>([]);
  const [categoriesLoading, setCategoriesLoading] = useState(false);
  const [categoriesError, setCategoriesError] = useState<string | null>(null);
  const [ruleDrafts, setRuleDrafts] = useState<Record<string, RuleDraft>>({});
  const [savingRuleFor, setSavingRuleFor] = useState<string | null>(null);
  const [ruleSaveError, setRuleSaveError] = useState<string | null>(null);
  const stagedDiagnostics = useMemo(() => {
    if (stagedRows.length === 0) {
      return null;
    }
    const sortedByDate = [...stagedRows].sort((left, right) =>
      left.bookingDate.localeCompare(right.bookingDate),
    );
    return {
      count: stagedRows.length,
      firstDate: sortedByDate[0].bookingDate,
      lastDate: sortedByDate[sortedByDate.length - 1].bookingDate,
    };
  }, [stagedRows]);
  const approvedCount = useMemo(
    () => stagedRows.filter((row) => row.isApproved).length,
    [stagedRows],
  );

  useEffect(() => {
    const handlePopState = () => {
      setRoute(getRouteFromPath());
    };
    window.addEventListener("popstate", handlePopState);
    return () => window.removeEventListener("popstate", handlePopState);
  }, [getRouteFromPath]);

  const navigate = useCallback(
    (next: Route) => {
      const nextPath = next === "dashboard" ? "/" : `/${next}`;
      window.history.pushState({}, "", nextPath);
      setRoute(next);
      if (next !== "imports") {
        setImportView("list");
      }
    },
    [setRoute],
  );

  const navigationWidget = (
    <div className="nav-widget" aria-label="Quick navigation widget">
      <span className="nav-widget__label">Quick navigation</span>
      <div className="nav-widget__links">
        <button
          className={`nav-widget__link ${route === "dashboard" ? "nav-widget__link--active" : ""}`}
          type="button"
          onClick={() => navigate("dashboard")}
        >
          Dashboard
        </button>
        <button
          className={`nav-widget__link ${route === "imports" ? "nav-widget__link--active" : ""}`}
          type="button"
          onClick={() => navigate("imports")}
        >
          Imports
        </button>
        <button
          className={`nav-widget__link ${route === "rules" ? "nav-widget__link--active" : ""}`}
          type="button"
          onClick={() => navigate("rules")}
        >
          Rules
        </button>
      </div>
    </div>
  );

  const loadTransactions = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await fetch("/api/transactions");
      if (!response.ok) {
        throw new Error("Unable to load transactions.");
      }
      const data = (await response.json()) as Transaction[];
      setTransactions(data);
    } catch (fetchError) {
      const message =
        fetchError instanceof Error
          ? fetchError.message
          : "Unexpected error while loading transactions.";
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const loadImports = useCallback(async () => {
    setImportsLoading(true);
    setImportsError(null);
    try {
      const response = await fetch("/api/imports");
      if (!response.ok) {
        throw new Error("Unable to load imports.");
      }
      const data = (await response.json()) as ImportBatch[];
      setImports(data);
    } catch (fetchError) {
      const message =
        fetchError instanceof Error
          ? fetchError.message
          : "Unexpected error while loading imports.";
      setImportsError(message);
    } finally {
      setImportsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadTransactions();
    loadImports();
  }, [loadImports, loadTransactions]);

  useEffect(() => {
    localStorage.setItem("rulesMonth", rulesMonth);
  }, [rulesMonth]);

  const formattedImports = useMemo(
    () =>
      imports.map((item) => ({
        ...item,
        displayMonth: item.statementMonth.slice(0, 7),
        displayUploadedAt: new Date(item.uploadedAt).toLocaleString(),
      })),
    [imports],
  );

  const shiftMonth = useCallback((value: string, delta: number) => {
    const [year, month] = value.split("-").map(Number);
    const absoluteMonth = year * 12 + (month - 1) + delta;
    const nextYear = Math.floor(absoluteMonth / 12);
    const nextMonth = (absoluteMonth % 12) + 1;

    return `${nextYear.toString().padStart(4, "0")}-${nextMonth
      .toString()
      .padStart(2, "0")}`;
  }, []);

  const renderTopNav = (currentRoute: Route) => (
    <nav className="nav">
      <a
        className={`nav__link ${
          currentRoute === "dashboard" ? "nav__link--active" : ""
        }`}
        href="/"
        onClick={(event) => {
          event.preventDefault();
          navigate("dashboard");
        }}
      >
        Dashboard
      </a>
      <a
        className={`nav__link ${
          currentRoute === "imports" ? "nav__link--active" : ""
        }`}
        href="/imports"
        onClick={(event) => {
          event.preventDefault();
          navigate("imports");
        }}
      >
        Imports
      </a>
      <a
        className={`nav__link ${
          currentRoute === "rules" ? "nav__link--active" : ""
        }`}
        href="/rules"
        onClick={(event) => {
          event.preventDefault();
          navigate("rules");
        }}
      >
        Rules
      </a>
    </nav>
  );

  const toTitleCase = useCallback((value: string) => {
    return value
      .trim()
      .split(/\s+/)
      .map((word) => {
        const lower = word.toLowerCase();
        return lower.charAt(0).toUpperCase() + lower.slice(1);
      })
      .join(" ");
  }, []);

  const selectedImport = useMemo(
    () => formattedImports.find((item) => item.id === selectedImportId) ?? null,
    [formattedImports, selectedImportId],
  );

  const loadExtractedText = useCallback(async (importId: number) => {
    setExtractedTextLoading(true);
    setExtractedTextError(null);
    try {
      const response = await fetch(`/api/imports/${importId}/extracted-text`);
      if (response.status === 404) {
        setExtractedText(null);
        return;
      }
      if (!response.ok) {
        throw new Error("Unable to load extracted text.");
      }
      const text = await response.text();
      setExtractedText(text);
    } catch (fetchError) {
      const message =
        fetchError instanceof Error
          ? fetchError.message
          : "Unexpected error while loading extracted text.";
      setExtractedTextError(message);
    } finally {
      setExtractedTextLoading(false);
    }
  }, []);

  const loadStagedRows = useCallback(async (importId: number) => {
    setStagedLoading(true);
    setStagedError(null);
    try {
      const response = await fetch(
        `/api/imports/${importId}/staged-transactions`,
      );
      if (response.status === 404) {
        setStagedRows([]);
        return;
      }
      if (!response.ok) {
        throw new Error("Unable to load staged transactions.");
      }
      const rows = (await response.json()) as StagedTransaction[];
      setStagedRows(rows);
    } catch (fetchError) {
      const message =
        fetchError instanceof Error
          ? fetchError.message
          : "Unexpected error while loading staged transactions.";
      setStagedError(message);
    } finally {
      setStagedLoading(false);
    }
  }, []);

  const loadCategories = useCallback(async () => {
    setCategoriesLoading(true);
    setCategoriesError(null);
    try {
      const response = await fetch("/api/categories");
      if (!response.ok) {
        throw new Error("Unable to load categories.");
      }
      const data = (await response.json()) as Category[];
      setCategories(data);
    } catch (fetchError) {
      const message =
        fetchError instanceof Error
          ? fetchError.message
          : "Unexpected error while loading categories.";
      setCategoriesError(message);
    } finally {
      setCategoriesLoading(false);
    }
  }, []);

  const loadRules = useCallback(async () => {
    setRulesLoading(true);
    setRulesError(null);
    try {
      const response = await fetch("/api/rules");
      if (!response.ok) {
        throw new Error("Unable to load rules.");
      }
      const data = (await response.json()) as MerchantRule[];
      setMerchantRules(data);
    } catch (fetchError) {
      const message =
        fetchError instanceof Error
          ? fetchError.message
          : "Unexpected error while loading rules.";
      setRulesError(message);
    } finally {
      setRulesLoading(false);
    }
  }, []);

  const loadUnmappedMerchants = useCallback(
    async (month: string) => {
      setUnmappedLoading(true);
      setUnmappedError(null);
      try {
        const response = await fetch("/api/rules/unmapped-merchants", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ month }),
        });
        if (!response.ok) {
          const errorPayload = (await response.json()) as { error?: string };
          throw new Error(
            errorPayload.error ?? "Unable to load unmapped merchants.",
          );
        }
        const data = (await response.json()) as string[];
        setUnmappedMerchants(data);
      } catch (fetchError) {
        const message =
          fetchError instanceof Error
            ? fetchError.message
            : "Unexpected error while loading unmapped merchants.";
        setUnmappedError(message);
      } finally {
        setUnmappedLoading(false);
      }
    },
    [setUnmappedMerchants],
  );

  const handleExtractText = useCallback(async () => {
    if (!selectedImport) {
      return;
    }

    setIsExtracting(true);
    setExtractedTextError(null);
    try {
      const response = await fetch(
        `/api/imports/${selectedImport.id}/extract-text`,
        {
          method: "POST",
        },
      );
      if (!response.ok) {
        const errorPayload = (await response.json()) as { error?: string };
        throw new Error(
          errorPayload.error ?? "Unable to extract text from the PDF.",
        );
      }

      await loadImports();
      await loadExtractedText(selectedImport.id);
    } catch (extractError) {
      const message =
        extractError instanceof Error
          ? extractError.message
          : "Unexpected error while extracting text.";
      setExtractedTextError(message);
    } finally {
      setIsExtracting(false);
    }
  }, [loadExtractedText, loadImports, selectedImport]);

  const handleParseToStaging = useCallback(async () => {
    if (!selectedImport) {
      return;
    }

    setIsParsing(true);
    setStagedError(null);
    try {
      const response = await fetch(
        `/api/imports/${selectedImport.id}/parse-to-staging`,
        {
          method: "POST",
        },
      );
      if (!response.ok) {
        const errorPayload = (await response.json()) as { error?: string };
        throw new Error(
          errorPayload.error ?? "Unable to parse staged transactions.",
        );
      }

      const rows = (await response.json()) as StagedTransaction[];
      setStagedRows(rows);
      setCommitMessage(null);
      setCommitError(null);
      await loadImports();
    } catch (parseError) {
      const message =
        parseError instanceof Error
          ? parseError.message
          : "Unexpected error while parsing staged transactions.";
      setStagedError(message);
    } finally {
      setIsParsing(false);
    }
  }, [loadImports, selectedImport]);

  const handleApprovalChange = useCallback(
    async (rowId: number, nextValue: boolean) => {
      if (!selectedImport) {
        return;
      }

      setStagedError(null);
      try {
        const response = await fetch(
          `/api/imports/${selectedImport.id}/staged-transactions/${rowId}/approval`,
          {
            method: "PUT",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify({ isApproved: nextValue }),
          },
        );

        if (!response.ok) {
          const errorPayload = (await response.json()) as { error?: string };
          throw new Error(
            errorPayload.error ?? "Unable to update the approval status.",
          );
        }

        const updated = (await response.json()) as StagedTransaction;
        setStagedRows((previous) =>
          previous.map((row) => (row.id === updated.id ? updated : row)),
        );
      } catch (approvalError) {
        const message =
          approvalError instanceof Error
            ? approvalError.message
            : "Unexpected error while updating approvals.";
        setStagedError(message);
      }
    },
    [selectedImport],
  );

  const handleCommitApproved = useCallback(async () => {
    if (!selectedImport) {
      return;
    }

    setIsCommitting(true);
    setCommitMessage(null);
    setCommitError(null);
    try {
      const response = await fetch(`/api/imports/${selectedImport.id}/commit`, {
        method: "POST",
      });

      if (!response.ok) {
        const errorPayload = (await response.json()) as { error?: string };
        throw new Error(
          errorPayload.error ?? "Unable to commit approved transactions.",
        );
      }

      const result = (await response.json()) as CommitImportResult;
      setCommitMessage(
        `Committed ${result.committedCount} of ${result.approvedCount} approved rows (skipped ${result.skippedCount}).`,
      );
      await loadImports();
      await loadTransactions();
      await loadStagedRows(selectedImport.id);
    } catch (commitError) {
      const message =
        commitError instanceof Error
          ? commitError.message
          : "Unexpected error while committing transactions.";
      setCommitError(message);
    } finally {
      setIsCommitting(false);
    }
  }, [loadImports, loadStagedRows, loadTransactions, selectedImport]);

  const handleSaveRule = useCallback(
    async (merchant: string) => {
      const draft = ruleDrafts[merchant];
      if (!draft) {
        return;
      }

      setSavingRuleFor(merchant);
      setRuleSaveError(null);
      try {
        const response = await fetch("/api/rules", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            pattern: draft.pattern,
            matchType: "Contains",
            normalizedMerchant: draft.normalizedMerchant,
            categoryId: draft.categoryId ? Number(draft.categoryId) : undefined,
            priority: draft.priority ? Number(draft.priority) : undefined,
          }),
        });

        if (!response.ok) {
          const errorPayload = (await response.json()) as { error?: string };
          throw new Error(
            errorPayload.error ?? "Unable to save the rule.",
          );
        }

        await loadRules();
        await loadUnmappedMerchants(rulesMonth);
      } catch (saveError) {
        const message =
          saveError instanceof Error
            ? saveError.message
            : "Unexpected error while saving the rule.";
        setRuleSaveError(message);
      } finally {
        setSavingRuleFor(null);
      }
    },
    [loadRules, loadUnmappedMerchants, ruleDrafts, rulesMonth],
  );

  useEffect(() => {
    setExtractedText(null);
    setExtractedTextError(null);
    setExtractedTextLoading(false);
    setStagedRows([]);
    setStagedError(null);
    setStagedLoading(false);
    setCommitMessage(null);
    setCommitError(null);

    if (selectedImport?.extractedAtUtc) {
      void loadExtractedText(selectedImport.id);
      void loadStagedRows(selectedImport.id);
    }
  }, [loadExtractedText, loadStagedRows, selectedImport]);

  useEffect(() => {
    if (route !== "rules") {
      return;
    }

    void loadCategories();
    void loadRules();
    void loadUnmappedMerchants(rulesMonth);
  }, [loadCategories, loadRules, loadUnmappedMerchants, route, rulesMonth]);

  useEffect(() => {
    setRuleDrafts((previous) => {
      const next = { ...previous };
      for (const merchant of unmappedMerchants) {
        if (!next[merchant]) {
          next[merchant] = {
            pattern: merchant,
            normalizedMerchant: toTitleCase(merchant),
            categoryId: "",
            priority: "",
          };
        }
      }
      return next;
    });
  }, [toTitleCase, unmappedMerchants]);

  const handleUpload = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setUploadError(null);

    if (!selectedFile) {
      setUploadError("Choose a PDF file to upload.");
      return;
    }

    if (!statementMonth) {
      setUploadError("Select the statement month.");
      return;
    }

    const formData = new FormData();
    formData.append("pdf", selectedFile);
    formData.append("statementMonth", statementMonth);

    setIsUploading(true);

    try {
      const response = await fetch("/api/imports", {
        method: "POST",
        body: formData,
      });

      if (!response.ok) {
        const errorPayload = (await response.json()) as { error?: string };
        throw new Error(
          errorPayload.error ?? "Unable to upload the statement.",
        );
      }

      setSelectedFile(null);
      setStatementMonth("");
      await loadImports();
    } catch (uploadError) {
      const message =
        uploadError instanceof Error
          ? uploadError.message
          : "Unexpected error while uploading.";
      setUploadError(message);
    } finally {
      setIsUploading(false);
    }
  };

  if (isDashboardRoute) {
    return (
      <main className="app">
        <section className="card">
          <header className="card__header">
            <div>
              <h1>Finance Overview</h1>
              <p>Track statement imports and monitor transaction activity.</p>
            </div>
            {renderTopNav(route)}
          </header>
          {navigationWidget}
          <Dashboard
            transactions={transactions}
            isLoading={isLoading}
            error={error}
          />
        </section>
      </main>
    );
  }

  return (
    <main className="app">
      <section className="card">
        <header className="card__header">
          <div>
            <h1>Finance Overview</h1>
            <p>Track statement imports and monitor transaction activity.</p>
          </div>
          {renderTopNav(route)}
        </header>
        {navigationWidget}

        {route === "imports" ? (
          importView === "review" ? (
            <div className="import-review">
              <button
                className="button button--ghost"
                type="button"
                onClick={() => setImportView("list")}
              >
                ← Back to imports
              </button>
              <header>
                <h2>Import review</h2>
                <p>
                  Confirm the PDF metadata and review extracted text before
                  committing transactions.
                </p>
              </header>
              {selectedImport ? (
                <div className="import-review__grid">
                  <section className="import-review__card">
                    <h3>PDF metadata</h3>
                    <dl className="import-review__meta">
                      <dt>File name</dt>
                      <dd>{selectedImport.originalFileName}</dd>
                      <dt>Statement month</dt>
                      <dd>{selectedImport.displayMonth}</dd>
                      <dt>Uploaded</dt>
                      <dd>{selectedImport.displayUploadedAt}</dd>
                      <dt>Status</dt>
                      <dd>{selectedImport.status}</dd>
                      <dt>Storage key</dt>
                      <dd>{selectedImport.storageKey}</dd>
                      <dt>SHA-256 hash</dt>
                      <dd>{selectedImport.sha256Hash ?? "Pending"}</dd>
                    </dl>
                  </section>
                  <section className="import-review__card">
                    <div className="import-review__header">
                      <h3>Extracted text</h3>
                      {!selectedImport.extractedAtUtc ? (
                        <button
                          className="button button--ghost"
                          type="button"
                          onClick={handleExtractText}
                          disabled={isExtracting}
                        >
                          {isExtracting ? "Extracting…" : "Extract text"}
                        </button>
                      ) : (
                        <button
                          className="button button--ghost"
                          type="button"
                          onClick={handleParseToStaging}
                          disabled={isParsing}
                        >
                          {isParsing ? "Parsing…" : "Parse to staging"}
                        </button>
                      )}
                    </div>
                    {extractedTextLoading ? (
                      <p className="status">Loading extracted text…</p>
                    ) : extractedTextError ? (
                      <p className="status status--error">
                        {extractedTextError}
                      </p>
                    ) : extractedText ? (
                      <pre className="import-review__text">{extractedText}</pre>
                    ) : (
                      <div className="import-review__placeholder">
                        No extracted text available yet.
                      </div>
                    )}
                  </section>
                  <section className="import-review__card">
                    <div className="import-review__header">
                      <h3>Staged transactions</h3>
                      <button
                        className="button button--ghost"
                        type="button"
                        onClick={handleCommitApproved}
                        disabled={isCommitting || approvedCount === 0}
                      >
                        {isCommitting ? "Committing…" : "Commit approved"}
                      </button>
                    </div>
                    <div>
                      <h4>Diagnostics</h4>
                      <dl className="import-review__meta">
                        <dt>Import status</dt>
                        <dd>{selectedImport.status}</dd>
                        <dt>Detected format</dt>
                        <dd>{selectedImport.parserKey ?? "Not detected"}</dd>
                        <dt>Parsed rows</dt>
                        <dd>{stagedDiagnostics?.count ?? "Not parsed yet"}</dd>
                        <dt>First booking date</dt>
                        <dd>{stagedDiagnostics?.firstDate ?? "—"}</dd>
                        <dt>Last booking date</dt>
                        <dd>{stagedDiagnostics?.lastDate ?? "—"}</dd>
                      </dl>
                    </div>
                    {stagedLoading ? (
                      <p className="status">Loading staged transactions…</p>
                    ) : stagedError ? (
                      <p className="status status--error">{stagedError}</p>
                    ) : stagedRows.length === 0 ? (
                      <div className="import-review__placeholder">
                        No staged transactions yet.
                      </div>
                    ) : (
                      <div className="staged-table">
                        <div className="staged-table__row staged-table__row--head">
                          <span>Approve</span>
                          <span>Date</span>
                          <span>Description</span>
                          <span>Amount</span>
                          <span>Balance</span>
                        </div>
                        {stagedRows.map((row) => (
                          <div className="staged-table__row" key={row.id}>
                            <label className="staged-table__approval">
                              <input
                                type="checkbox"
                                checked={row.isApproved}
                                onChange={(event) =>
                                  handleApprovalChange(
                                    row.id,
                                    event.target.checked,
                                  )
                                }
                              />
                              <span>
                                {row.isApproved ? "Approved" : "Rejected"}
                              </span>
                            </label>
                            <span>{row.bookingDate}</span>
                            <span>{row.rawDescription}</span>
                            <span>
                              {row.amount.toFixed(2)} {row.currency ?? "EUR"}
                            </span>
                            <span>
                              {row.runningBalance === null
                                ? "—"
                                : row.runningBalance.toFixed(2)}
                            </span>
                          </div>
                        ))}
                      </div>
                    )}
                    {commitMessage ? (
                      <p className="status status--success">{commitMessage}</p>
                    ) : null}
                    {commitError ? (
                      <p className="status status--error">{commitError}</p>
                    ) : null}
                  </section>
                </div>
              ) : (
                <p className="status">Select an import to review.</p>
              )}
            </div>
          ) : (
            <div className="imports">
              <form className="imports__form" onSubmit={handleUpload}>
                <div className="field">
                  <label htmlFor="statement-file">Statement PDF</label>
                  <input
                    id="statement-file"
                    type="file"
                    accept="application/pdf"
                    onChange={(event) =>
                      setSelectedFile(event.target.files?.[0] ?? null)
                    }
                  />
                </div>
                <div className="field">
                  <label htmlFor="statement-month">Statement month</label>
                  <input
                    id="statement-month"
                    type="month"
                    value={statementMonth}
                    onChange={(event) => setStatementMonth(event.target.value)}
                  />
                </div>
                <button
                  className="button"
                  type="submit"
                  disabled={isUploading}
                >
                  {isUploading ? "Uploading…" : "Upload statement"}
                </button>
                {uploadError ? (
                  <p className="status status--error">{uploadError}</p>
                ) : null}
              </form>

              <div className="imports__list">
                <h2>Previous uploads</h2>
                {importsLoading ? (
                  <p className="status">Loading imports…</p>
                ) : importsError ? (
                  <p className="status status--error">{importsError}</p>
                ) : formattedImports.length === 0 ? (
                  <p className="status">No imports yet.</p>
                ) : (
                  <ul className="import-list">
                    {formattedImports.map((item) => (
                      <li className="import-list__item" key={item.id}>
                        <div>
                          <div className="import-list__filename">
                            {item.originalFileName}
                          </div>
                          <div className="import-list__meta">
                            {item.displayMonth} • Uploaded{" "}
                            {item.displayUploadedAt}
                          </div>
                        </div>
                        <div className="import-list__actions">
                          <span className="badge">{item.status}</span>
                          <button
                            className="button button--ghost"
                            type="button"
                            onClick={() => {
                              setSelectedImportId(item.id);
                              setImportView("review");
                            }}
                          >
                            Review
                          </button>
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          )
        ) : route === "rules" ? (
          <div className="rules">
            <section className="rules__month">
              <h2>Rules</h2>
              <div className="month-selector">
                <button
                  className="button button--ghost"
                  type="button"
                  onClick={() => setRulesMonth((current) => shiftMonth(current, -1))}
                >
                  ←
                </button>
                <input
                  type="month"
                  value={rulesMonth}
                  onChange={(event) => setRulesMonth(event.target.value)}
                />
                <button
                  className="button button--ghost"
                  type="button"
                  onClick={() => setRulesMonth((current) => shiftMonth(current, 1))}
                >
                  →
                </button>
              </div>
            </section>

            <section className="rules__section">
              <div className="rules__header">
                <h3>Unmapped merchants (this month)</h3>
                <button
                  className="button button--ghost"
                  type="button"
                  onClick={() => loadUnmappedMerchants(rulesMonth)}
                  disabled={unmappedLoading}
                >
                  {unmappedLoading ? "Refreshing…" : "Refresh"}
                </button>
              </div>
              {unmappedError ? (
                <p className="status status--error">{unmappedError}</p>
              ) : null}
              {unmappedLoading ? (
                <p className="status">Loading unmapped merchants…</p>
              ) : unmappedMerchants.length === 0 ? (
                <p className="status">No unmapped merchants for this month.</p>
              ) : (
                <div className="rules__list">
                  {unmappedMerchants.map((merchant) => {
                    const draft = ruleDrafts[merchant];
                    return (
                      <div className="rules__card" key={merchant}>
                        <div className="rules__candidate">{merchant}</div>
                        <div className="rules__form">
                          <label>
                            Pattern
                            <input
                              type="text"
                              value={draft?.pattern ?? merchant}
                              onChange={(event) =>
                                setRuleDrafts((previous) => ({
                                  ...previous,
                                  [merchant]: {
                                    ...(previous[merchant] ?? {
                                      pattern: merchant,
                                      normalizedMerchant: toTitleCase(merchant),
                                      categoryId: "",
                                      priority: "",
                                    }),
                                    pattern: event.target.value,
                                  },
                                }))
                              }
                            />
                          </label>
                          <label>
                            Normalized merchant
                            <input
                              type="text"
                              value={draft?.normalizedMerchant ?? ""}
                              onChange={(event) =>
                                setRuleDrafts((previous) => ({
                                  ...previous,
                                  [merchant]: {
                                    ...(previous[merchant] ?? {
                                      pattern: merchant,
                                      normalizedMerchant: toTitleCase(merchant),
                                      categoryId: "",
                                      priority: "",
                                    }),
                                    normalizedMerchant: event.target.value,
                                  },
                                }))
                              }
                            />
                          </label>
                          <label>
                            Category
                            <select
                              value={draft?.categoryId ?? ""}
                              onChange={(event) =>
                                setRuleDrafts((previous) => ({
                                  ...previous,
                                  [merchant]: {
                                    ...(previous[merchant] ?? {
                                      pattern: merchant,
                                      normalizedMerchant: toTitleCase(merchant),
                                      categoryId: "",
                                      priority: "",
                                    }),
                                    categoryId: event.target.value,
                                  },
                                }))
                              }
                            >
                              <option value="">Uncategorized</option>
                              {categories.map((category) => (
                                <option key={category.id} value={category.id}>
                                  {category.name}
                                </option>
                              ))}
                            </select>
                          </label>
                          <label>
                            Priority
                            <input
                              type="number"
                              value={draft?.priority ?? ""}
                              onChange={(event) =>
                                setRuleDrafts((previous) => ({
                                  ...previous,
                                  [merchant]: {
                                    ...(previous[merchant] ?? {
                                      pattern: merchant,
                                      normalizedMerchant: toTitleCase(merchant),
                                      categoryId: "",
                                      priority: "",
                                    }),
                                    priority: event.target.value,
                                  },
                                }))
                              }
                            />
                          </label>
                        </div>
                        <button
                          className="button"
                          type="button"
                          onClick={() => handleSaveRule(merchant)}
                          disabled={savingRuleFor === merchant}
                        >
                          {savingRuleFor === merchant ? "Saving…" : "Save rule"}
                        </button>
                      </div>
                    );
                  })}
                </div>
              )}
              {ruleSaveError ? (
                <p className="status status--error">{ruleSaveError}</p>
              ) : null}
              {categoriesError ? (
                <p className="status status--error">{categoriesError}</p>
              ) : null}
              {categoriesLoading ? (
                <p className="status">Loading categories…</p>
              ) : null}
            </section>

            <section className="rules__section">
              <h3>Existing rules</h3>
              {rulesError ? (
                <p className="status status--error">{rulesError}</p>
              ) : null}
              {rulesLoading ? (
                <p className="status">Loading rules…</p>
              ) : merchantRules.length === 0 ? (
                <p className="status">No rules yet.</p>
              ) : (
                <ul className="rules__existing">
                  {merchantRules.map((rule) => (
                    <li key={rule.id}>
                      <strong>{rule.pattern}</strong> → {rule.normalizedMerchant}
                      {rule.categoryName ? ` → ${rule.categoryName}` : ""}
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </div>
        ) : null}
      </section>
    </main>
  );
};

export default App;
