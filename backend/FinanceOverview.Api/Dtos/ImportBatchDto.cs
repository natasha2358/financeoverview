namespace FinanceOverview.Api.Dtos;

public sealed record ImportBatchDto(
    int Id,
    DateTime UploadedAt,
    string OriginalFileName,
    DateOnly StatementMonth,
    string Status,
    string StorageKey,
    string? Sha256Hash);
