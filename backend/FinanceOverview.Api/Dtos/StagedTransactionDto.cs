namespace FinanceOverview.Api.Dtos;

public sealed record StagedTransactionDto(
    int Id,
    int ImportBatchId,
    int RowIndex,
    DateOnly BookingDate,
    DateOnly? ValueDate,
    string RawDescription,
    decimal Amount,
    string? Currency,
    decimal? RunningBalance,
    bool IsApproved);
