type Transaction = {
  id: number;
  date: string;
  rawDescription: string;
  merchant: string;
  amount: number;
  currency: string;
  balance: number | null;
};

type DashboardProps = {
  transactions: Transaction[];
  isLoading: boolean;
  error: string | null;
};

const Dashboard = ({ transactions, isLoading, error }: DashboardProps) => {
  return (
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
  );
};

export default Dashboard;
