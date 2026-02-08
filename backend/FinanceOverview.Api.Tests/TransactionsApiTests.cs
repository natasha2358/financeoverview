using System.Net;
using System.Net.Http.Json;
using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceOverview.Api.Tests;

public class TransactionsApiTests
{
    [Fact]
    public async Task Get_EmptyDatabase_ReturnsEmptyArray()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();

        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task Get_SeededData_ReturnsExpectedDtoShape()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        Transaction olderTransaction;
        Transaction newerTransaction;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            olderTransaction = new Transaction
            {
                Date = new DateOnly(2024, 1, 5),
                RawDescription = "Morning coffee",
                Merchant = "Cafe Central",
                Amount = -3.50m,
                Currency = "EUR",
                Balance = 120.75m
            };
            newerTransaction = new Transaction
            {
                Date = new DateOnly(2024, 1, 6),
                RawDescription = "Weekly salary",
                Merchant = "Contoso Ltd",
                Amount = 850.00m,
                Currency = "EUR",
                Balance = 970.75m
            };

            dbContext.Transactions.AddRange(olderTransaction, newerTransaction);
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        var payload = await client.GetFromJsonAsync<List<TransactionDto>>("/api/transactions");

        Assert.NotNull(payload);
        Assert.Equal(2, payload.Count);
        Assert.Equal(newerTransaction.Id, payload[0].Id);
        Assert.Equal(newerTransaction.Date, payload[0].Date);
        Assert.Equal(newerTransaction.RawDescription, payload[0].RawDescription);
        Assert.Equal(newerTransaction.Merchant, payload[0].Merchant);
        Assert.Equal(newerTransaction.Amount, payload[0].Amount);
        Assert.Equal(newerTransaction.Currency, payload[0].Currency);
        Assert.Equal(newerTransaction.Balance, payload[0].Balance);

        Assert.Equal(olderTransaction.Id, payload[1].Id);
        Assert.Equal(olderTransaction.Date, payload[1].Date);
        Assert.Equal(olderTransaction.RawDescription, payload[1].RawDescription);
        Assert.Equal(olderTransaction.Merchant, payload[1].Merchant);
        Assert.Equal(olderTransaction.Amount, payload[1].Amount);
        Assert.Equal(olderTransaction.Currency, payload[1].Currency);
        Assert.Equal(olderTransaction.Balance, payload[1].Balance);
    }

    [Fact]
    public async Task GetById_MissingTransaction_ReturnsNotFound()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/transactions/404");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesTransaction_ReturnsCreatedWithLocation()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var request = new CreateTransactionRequest
        {
            Date = new DateOnly(2024, 2, 1),
            RawDescription = "Lunch",
            Merchant = "Cafe Uno",
            Amount = -12.50m,
            Currency = "EUR",
            Balance = 150.25m
        };

        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/transactions", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var payload = await response.Content.ReadFromJsonAsync<TransactionDto>();

        Assert.NotNull(payload);
        Assert.True(payload.Id > 0);
        Assert.EndsWith($"/api/transactions/{payload.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(request.Date, payload.Date);
        Assert.Equal(request.RawDescription, payload.RawDescription);
        Assert.Equal(request.Merchant, payload.Merchant);
        Assert.Equal(request.Amount, payload.Amount);
        Assert.Equal(request.Currency, payload.Currency);
        Assert.Equal(request.Balance, payload.Balance);

        using var getResponse = await client.GetAsync($"/api/transactions/{payload.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }
}
