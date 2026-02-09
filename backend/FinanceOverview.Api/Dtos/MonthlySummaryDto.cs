namespace FinanceOverview.Api.Dtos;

public sealed record MonthlySummaryDto(
    DateOnly Month,
    decimal Income,
    decimal Expenses,
    decimal Net);
