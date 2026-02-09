using System.Net;
using System.Net.Http.Json;
using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceOverview.Api.Tests;

public class RulesApiTests
{
    [Fact]
    public async Task PostUnmappedMerchants_ReturnsDistinctCandidatesForMonth()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Transactions.AddRange(
                new Transaction
                {
                    Date = new DateOnly(2026, 2, 1),
                    RawDescription = "Coffee Shop Main Street",
                    Merchant = "Coffee Shop",
                    Amount = -4.5m,
                    Currency = "EUR"
                },
                new Transaction
                {
                    Date = new DateOnly(2026, 2, 2),
                    RawDescription = "  Coffee   Shop Main   Street ",
                    Merchant = "Coffee Shop",
                    Amount = -4.5m,
                    Currency = "EUR"
                },
                new Transaction
                {
                    Date = new DateOnly(2026, 2, 5),
                    RawDescription = "Rent payment February",
                    Merchant = "Rent",
                    Amount = -800m,
                    Currency = "EUR"
                },
                new Transaction
                {
                    Date = new DateOnly(2026, 1, 30),
                    RawDescription = "Outside month",
                    Merchant = "Outside",
                    Amount = -10m,
                    Currency = "EUR"
                });
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/rules/unmapped-merchants",
            new UnmappedMerchantsRequest { Month = "2026-02" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.Count);
        Assert.Contains("Coffee Shop Main Street", payload);
        Assert.Contains("Rent payment February", payload);
    }
}
