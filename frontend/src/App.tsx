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
  originalFileName: string;
  statementMonth: string;
  status: string;
  storageKey: string;
  sha256Hash: string | null;
};

type ViewMode = "transactions" | "imports";

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
                      <span className="badge">{item.status}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
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
