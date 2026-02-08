using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Models;
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

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create([FromBody] CreateTransactionRequest request)
    {
        var transaction = new Transaction
        {
            Date = request.Date,
            RawDescription = request.RawDescription.Trim(),
            Merchant = request.Merchant?.Trim() ?? string.Empty,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency.Trim(),
            Balance = request.Balance
        };

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        var response = new TransactionDto(
            transaction.Id,
            transaction.Date,
            transaction.RawDescription,
            transaction.Merchant,
            transaction.Amount,
            transaction.Currency,
            transaction.Balance);

        return Created($"/api/transactions/{transaction.Id}", response);
    }
}
