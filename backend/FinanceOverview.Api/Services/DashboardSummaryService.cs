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

        var amounts = await transactions
            .Select(transaction => transaction.Amount)
            .ToListAsync(cancellationToken);

        var incomeTotal = amounts.Where(amount => amount > 0).Sum();
        var expenseTotal = Math.Abs(amounts.Where(amount => amount < 0).Sum());
        var netTotal = amounts.Sum();
        var transactionCount = amounts.Count;

        return new MonthlySummaryResponseDto(
            monthStart.ToString("yyyy-MM"),
            incomeTotal,
            expenseTotal,
            netTotal,
            transactionCount);
    }
}
