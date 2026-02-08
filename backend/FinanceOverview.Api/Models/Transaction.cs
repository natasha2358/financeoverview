namespace FinanceOverview.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string RawDescription { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal? Balance { get; set; }
}
