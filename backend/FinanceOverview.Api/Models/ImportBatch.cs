namespace FinanceOverview.Api.Models;

public class ImportBatch
{
    public int Id { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ExtractedAtUtc { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public DateOnly StatementMonth { get; set; }
    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Uploaded;
    public string StorageKey { get; set; } = string.Empty;
    public string? Sha256Hash { get; set; }
    public string? ParserKey { get; set; }
    public int? ParsedRowCount { get; set; }
    public DateOnly? FirstBookingDate { get; set; }
    public DateOnly? LastBookingDate { get; set; }
}

public enum ImportBatchStatus
{
    Uploaded,
    Extracted,
    Parsed,
    Committed,
    Failed
}
