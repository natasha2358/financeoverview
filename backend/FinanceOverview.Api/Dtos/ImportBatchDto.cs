namespace FinanceOverview.Api.Dtos;

public sealed record ImportBatchDto(
    int Id,
    DateTime UploadedAt,
    DateTime? ExtractedAtUtc,
    string OriginalFileName,
    DateOnly StatementMonth,
    string Status,
    string StorageKey,
    string? Sha256Hash,
    string? ParserKey,
    int? ParsedRowCount,
    DateOnly? FirstBookingDate,
    DateOnly? LastBookingDate);
