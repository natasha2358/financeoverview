namespace FinanceOverview.Api.Dtos;

public sealed record MonthlySummaryResponseDto(
    string Month,
    decimal IncomeTotal,
    decimal ExpenseTotal,
    decimal NetTotal,
    int TransactionCount);
