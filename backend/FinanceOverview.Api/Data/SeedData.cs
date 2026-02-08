using FinanceOverview.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceOverview.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();

        if (await dbContext.Transactions.AnyAsync())
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var seedTransactions = new List<Transaction>
        {
            new()
            {
                Date = today.AddDays(-2),
                RawDescription = "Coffee shop purchase",
                Merchant = "Bluebird Cafe",
                Amount = -4.8m,
                Currency = "EUR",
                Balance = 1520.35m
            },
            new()
            {
                Date = today.AddDays(-1),
                RawDescription = "Salary October",
                Merchant = "Acme Corp",
                Amount = 2450.0m,
                Currency = "EUR",
                Balance = 3970.35m
            },
            new()
            {
                Date = today,
                RawDescription = "Grocery store",
                Merchant = "Green Market",
                Amount = -62.45m,
                Currency = "EUR",
                Balance = 3907.90m
            }
        };

        dbContext.Transactions.AddRange(seedTransactions);
        await dbContext.SaveChangesAsync();
    }
}
