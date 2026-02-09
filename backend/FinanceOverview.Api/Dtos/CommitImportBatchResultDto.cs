namespace FinanceOverview.Api.Dtos;

public sealed record CommitImportBatchResultDto(
    int ImportBatchId,
    int ApprovedCount,
    int CommittedCount,
    int SkippedCount,
    string Status);
