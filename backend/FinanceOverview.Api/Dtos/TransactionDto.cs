namespace FinanceOverview.Api.Dtos;

public sealed record TransactionDto(
    int Id,
    DateOnly Date,
    string RawDescription,
    string Merchant,
    decimal Amount,
    string Currency,
    decimal? Balance);
