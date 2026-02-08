using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceOverview.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public TransactionsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> Get()
    {
        var transactions = await _dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(transaction => transaction.Date)
            .Select(transaction => new TransactionDto(
                transaction.Id,
                transaction.Date,
                transaction.RawDescription,
                transaction.Merchant,
                transaction.Amount,
                transaction.Currency,
                transaction.Balance))
            .ToListAsync();

        return Ok(transactions);
    }
}
