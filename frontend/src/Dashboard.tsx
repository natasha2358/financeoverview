import { useEffect, useMemo, useState } from "react";

const storageKey = "dashboardMonth";

type MonthlySummary = {
  month: string;
  incomeTotal: number;
  expenseTotal: number;
  netTotal: number;
  transactionCount: number;
};

const monthPattern = /^\d{4}-\d{2}$/;

const getCurrentMonth = () => {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}`;
};

const addMonths = (month: string, delta: number) => {
  const [year, monthValue] = month.split("-").map(Number);
  const date = new Date(Date.UTC(year, monthValue - 1, 1));
  date.setUTCMonth(date.getUTCMonth() + delta);
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, "0")}`;
};

const formatMonthLabel = (month: string) => {
  const [year, monthValue] = month.split("-").map(Number);
  const date = new Date(Date.UTC(year, monthValue - 1, 1));
  return date.toLocaleString(undefined, { month: "long", year: "numeric" });
};

const resolveInitialMonth = () => {
  const stored = localStorage.getItem(storageKey);
  if (stored && monthPattern.test(stored)) {
    return stored;
  }
  return getCurrentMonth();
};

const Dashboard = () => {
  const [selectedMonth, setSelectedMonth] = useState(resolveInitialMonth);
  const [summary, setSummary] = useState<MonthlySummary | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const currencyFormatter = useMemo(
    () =>
      new Intl.NumberFormat(undefined, {
        style: "currency",
        currency: "EUR",
      }),
    [],
  );

  useEffect(() => {
    localStorage.setItem(storageKey, selectedMonth);
  }, [selectedMonth]);

  useEffect(() => {
    let isActive = true;
    const loadSummary = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const response = await fetch("/api/dashboard/monthly-summary", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ month: selectedMonth }),
        });

        if (!response.ok) {
          throw new Error("Unable to load dashboard summary.");
        }

        const payload = (await response.json()) as MonthlySummary;
        if (isActive) {
          setSummary(payload);
        }
      } catch (fetchError) {
        const message =
          fetchError instanceof Error
            ? fetchError.message
            : "Unexpected error while loading the dashboard.";
        if (isActive) {
          setError(message);
          setSummary(null);
        }
      } finally {
        if (isActive) {
          setIsLoading(false);
        }
      }
    };

    loadSummary();

    return () => {
      isActive = false;
    };
  }, [selectedMonth]);

  const formattedMonth = formatMonthLabel(selectedMonth);

  return (
    <div className="app">
      <div className="card">
        <header className="card__header">
          <h1>Dashboard</h1>
          <p>Monthly overview of committed transactions.</p>
        </header>

        <section className="dashboard">
          <div className="dashboard__nav">
            <button
              className="dashboard__nav-button"
              type="button"
              onClick={() => setSelectedMonth((value) => addMonths(value, -1))}
              aria-label="Previous month"
            >
              ←
            </button>
            <div className="dashboard__month">{formattedMonth}</div>
            <button
              className="dashboard__nav-button"
              type="button"
              onClick={() => setSelectedMonth((value) => addMonths(value, 1))}
              aria-label="Next month"
            >
              →
            </button>
          </div>

          {isLoading && <p className="status">Loading summary…</p>}
          {error && <p className="status status--error">{error}</p>}

          {!isLoading && !error && summary && (
            <>
              <div className="dashboard__cards">
                <div className="dashboard__card">
                  <span className="dashboard__label">Income</span>
                  <span className="dashboard__value">
                    {currencyFormatter.format(summary.incomeTotal)}
                  </span>
                </div>
                <div className="dashboard__card">
                  <span className="dashboard__label">Expenses</span>
                  <span className="dashboard__value">
                    {currencyFormatter.format(summary.expenseTotal)}
                  </span>
                </div>
                <div className="dashboard__card">
                  <span className="dashboard__label">Net</span>
                  <span className="dashboard__value">
                    {currencyFormatter.format(summary.netTotal)}
                  </span>
                </div>
              </div>
              <p className="dashboard__count">
                Transactions: {summary.transactionCount}
              </p>
            </>
          )}
        </section>
      </div>
    </div>
  );
};

export default Dashboard;
