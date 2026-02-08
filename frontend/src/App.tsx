import { useEffect, useState } from "react";

type Transaction = {
  id: number;
  date: string;
  rawDescription: string;
  merchant: string;
  amount: number;
  currency: string;
  balance: number | null;
};

const App = () => {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadTransactions = async () => {
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
    };

    loadTransactions();
  }, []);

  return (
    <main className="app">
      <section className="card">
        <header className="card__header">
          <h1>Transactions</h1>
          <p>Latest activity from your imported statements.</p>
        </header>
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
      </section>
    </main>
  );
};

export default App;
