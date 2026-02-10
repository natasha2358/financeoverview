namespace FinanceOverview.Api.Models;

public class Transaction
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string RawDescription { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public string? MerchantNormalized { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal? Balance { get; set; }
    public int? ImportBatchId { get; set; }
    public string? RowFingerprint { get; set; }
}
