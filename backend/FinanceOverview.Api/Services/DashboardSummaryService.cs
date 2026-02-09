using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceOverview.Api.Services;

public sealed class DashboardSummaryService
{
    private readonly AppDbContext _dbContext;

    public DashboardSummaryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MonthlySummaryResponseDto> GetMonthlySummaryAsync(
        DateOnly monthStart,
        CancellationToken cancellationToken)
    {
        var monthEnd = monthStart.AddMonths(1);
        var committedImportBatchIds = _dbContext.ImportBatches
            .AsNoTracking()
            .Where(batch => batch.Status == ImportBatchStatus.Committed)
            .Select(batch => batch.Id);

        var transactions = _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Date >= monthStart && transaction.Date < monthEnd)
            .Where(transaction => transaction.ImportBatchId == null
                || committedImportBatchIds.Contains(transaction.ImportBatchId.Value));

        var summary = await transactions
            .GroupBy(_ => 1)
            .Select(group => new
            {
                IncomeTotal = group.Where(transaction => transaction.Amount > 0).Sum(transaction => transaction.Amount),
                ExpenseTotal = group.Where(transaction => transaction.Amount < 0).Sum(transaction => transaction.Amount),
                NetTotal = group.Sum(transaction => transaction.Amount),
                TransactionCount = group.Count()
            })
            .SingleOrDefaultAsync(cancellationToken);

        var incomeTotal = summary?.IncomeTotal ?? 0m;
        var expenseTotal = Math.Abs(summary?.ExpenseTotal ?? 0m);
        var netTotal = summary?.NetTotal ?? 0m;
        var transactionCount = summary?.TransactionCount ?? 0;

        return new MonthlySummaryResponseDto(
            monthStart.ToString("yyyy-MM"),
            incomeTotal,
            expenseTotal,
            netTotal,
            transactionCount);
    }
}
