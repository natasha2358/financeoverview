import type { FormEvent } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";

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

type ViewMode = "transactions" | "imports" | "review";

const App = () => {
  const [viewMode, setViewMode] = useState<ViewMode>("imports");
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

  const formattedImports = useMemo(
    () =>
      imports.map((item) => ({
        ...item,
        displayMonth: item.statementMonth.slice(0, 7),
        displayUploadedAt: new Date(item.uploadedAt).toLocaleString(),
      })),
    [imports],
  );

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

  useEffect(() => {
    setExtractedText(null);
    setExtractedTextError(null);
    setExtractedTextLoading(false);
    setStagedRows([]);
    setStagedError(null);
    setStagedLoading(false);

    if (selectedImport?.extractedAtUtc) {
      void loadExtractedText(selectedImport.id);
      void loadStagedRows(selectedImport.id);
    }
  }, [loadExtractedText, loadStagedRows, selectedImport]);

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

  return (
    <main className="app">
      <section className="card">
        <header className="card__header">
          <h1>Finance Overview</h1>
          <p>Track statement imports and monitor transaction activity.</p>
        </header>

        <div className="tabs">
          <button
            className={`tab ${viewMode === "imports" ? "tab--active" : ""}`}
            onClick={() => setViewMode("imports")}
            type="button"
          >
            Imports
          </button>
          <button
            className={`tab ${
              viewMode === "transactions" ? "tab--active" : ""
            }`}
            onClick={() => setViewMode("transactions")}
            type="button"
          >
            Transactions
          </button>
        </div>

        {viewMode === "imports" ? (
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
              <button className="button" type="submit" disabled={isUploading}>
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
                            setViewMode("review");
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
        ) : viewMode === "review" ? (
          <div className="import-review">
            <button
              className="button button--ghost"
              type="button"
              onClick={() => setViewMode("imports")}
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
                    <p className="status status--error">{extractedTextError}</p>
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
                        <span>Date</span>
                        <span>Description</span>
                        <span>Amount</span>
                        <span>Balance</span>
                      </div>
                      {stagedRows.map((row) => (
                        <div className="staged-table__row" key={row.id}>
                          <span>{row.bookingDate}</span>
                          <span>{row.rawDescription}</span>
                          <span>
                            {row.amount.toFixed(2)}{" "}
                            {row.currency ?? "EUR"}
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
                </section>
              </div>
            ) : (
              <p className="status">Select an import to review.</p>
            )}
          </div>
        ) : (
          <div className="transactions">
            {isLoading ? (
              <p className="status">Loading transactions…</p>
            ) : error ? (
              <p className="status status--error">{error}</p>
            ) : transactions.length === 0 ? (
              <p className="status">No transactions yet.</p>
            ) : (
              <ul className="transaction-list">
                {transactions.map((transaction) => (
                  <li className="transaction-list__item" key={transaction.id}>
                    <div>
                      <div className="transaction-list__merchant">
                        {transaction.merchant}
                      </div>
                      <div className="transaction-list__meta">
                        {transaction.date} • {transaction.rawDescription}
                      </div>
                    </div>
                    <div className="transaction-list__amount">
                      {transaction.amount.toFixed(2)} {transaction.currency}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </section>
    </main>
  );
};

export default App;
