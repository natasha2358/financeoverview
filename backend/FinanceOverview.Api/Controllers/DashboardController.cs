using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceOverview.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public DashboardController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("monthly-summary")]
    public async Task<ActionResult<IReadOnlyList<MonthlySummaryDto>>> PostMonthlySummary(
        [FromBody] MonthlySummaryRequest? request)
    {
        var query = _dbContext.Transactions.AsNoTracking();

        if (request?.StartDate is { } startDate)
        {
            query = query.Where(transaction => transaction.Date >= startDate);
        }

        if (request?.EndDate is { } endDate)
        {
            query = query.Where(transaction => transaction.Date <= endDate);
        }

        var transactionData = await query
            .Select(transaction => new { transaction.Date, transaction.Amount })
            .ToListAsync();

        var summaries = transactionData
            .GroupBy(transaction => new { transaction.Date.Year, transaction.Date.Month })
            .Select(group =>
            {
                var income = group.Where(entry => entry.Amount > 0).Sum(entry => entry.Amount);
                var expenses = group.Where(entry => entry.Amount < 0).Sum(entry => Math.Abs(entry.Amount));
                var net = income - expenses;
                var monthStart = new DateOnly(group.Key.Year, group.Key.Month, 1);

                return new MonthlySummaryDto(monthStart, income, expenses, net);
            })
            .OrderBy(summary => summary.Month)
            .ToList();

        return Ok(summaries);
    }
}
