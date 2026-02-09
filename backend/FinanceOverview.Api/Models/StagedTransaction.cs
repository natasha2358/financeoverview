namespace FinanceOverview.Api.Models;

public class StagedTransaction
{
    public int Id { get; set; }
    public int ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }
    public int RowIndex { get; set; }
    public DateOnly BookingDate { get; set; }
    public DateOnly? ValueDate { get; set; }
    public string RawDescription { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public decimal? RunningBalance { get; set; }
    public bool IsApproved { get; set; }
}
