using System.Net;
using System.Net.Http.Json;
using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceOverview.Api.Tests;

public class DashboardApiTests
{
    [Fact]
    public async Task Post_MonthlySummary_ReturnsTotalsForEachMonth()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var committedBatch = new ImportBatch
            {
                UploadedAt = new DateTime(2024, 1, 1),
                StatementMonth = new DateOnly(2024, 1, 1),
                Status = ImportBatchStatus.Committed,
                OriginalFileName = "jan.pdf",
                StorageKey = "jan.pdf"
            };
            var pendingBatch = new ImportBatch
            {
                UploadedAt = new DateTime(2024, 2, 1),
                StatementMonth = new DateOnly(2024, 2, 1),
                Status = ImportBatchStatus.Parsed,
                OriginalFileName = "feb.pdf",
                StorageKey = "feb.pdf"
            };

            dbContext.ImportBatches.AddRange(committedBatch, pendingBatch);
            await dbContext.SaveChangesAsync();

            dbContext.Transactions.AddRange(
                new Transaction
                {
                    Date = new DateOnly(2024, 1, 5),
                    RawDescription = "Salary",
                    Merchant = "Contoso",
                    Amount = 2000m,
                    Currency = "EUR",
                    ImportBatchId = committedBatch.Id
                },
                new Transaction
                {
                    Date = new DateOnly(2024, 1, 15),
                    RawDescription = "Groceries",
                    Merchant = "Market",
                    Amount = -150m,
                    Currency = "EUR",
                    ImportBatchId = committedBatch.Id
                },
                new Transaction
                {
                    Date = new DateOnly(2024, 1, 18),
                    RawDescription = "Ignored",
                    Merchant = "Ignore",
                    Amount = -45m,
                    Currency = "EUR",
                    ImportBatchId = pendingBatch.Id
                },
                new Transaction
                {
                    Date = new DateOnly(2024, 2, 3),
                    RawDescription = "Bonus",
                    Merchant = "Contoso",
                    Amount = 1500m,
                    Currency = "EUR",
                    ImportBatchId = committedBatch.Id
                },
                new Transaction
                {
                    Date = new DateOnly(2024, 2, 12),
                    RawDescription = "Rent",
                    Merchant = "Landlord",
                    Amount = -900m,
                    Currency = "EUR",
                    ImportBatchId = committedBatch.Id
                },
                new Transaction
                {
                    Date = new DateOnly(2024, 2, 20),
                    RawDescription = "Ignored",
                    Merchant = "Ignore",
                    Amount = -25m,
                    Currency = "EUR",
                    ImportBatchId = pendingBatch.Id
                });

            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient();

        var januaryResponse = await client.PostAsJsonAsync(
            "/api/dashboard/monthly-summary",
            new MonthlySummaryRequestDto { Month = "2024-01" });

        Assert.Equal(HttpStatusCode.OK, januaryResponse.StatusCode);

        var januaryPayload = await januaryResponse.Content.ReadFromJsonAsync<MonthlySummaryResponseDto>();

        Assert.NotNull(januaryPayload);
        Assert.Equal("2024-01", januaryPayload.Month);
        Assert.Equal(2000m, januaryPayload.IncomeTotal);
        Assert.Equal(150m, januaryPayload.ExpenseTotal);
        Assert.Equal(1850m, januaryPayload.NetTotal);
        Assert.Equal(2, januaryPayload.TransactionCount);

        var februaryResponse = await client.PostAsJsonAsync(
            "/api/dashboard/monthly-summary",
            new MonthlySummaryRequestDto { Month = "2024-02" });

        Assert.Equal(HttpStatusCode.OK, februaryResponse.StatusCode);

        var februaryPayload = await februaryResponse.Content.ReadFromJsonAsync<MonthlySummaryResponseDto>();

        Assert.NotNull(februaryPayload);
        Assert.Equal("2024-02", februaryPayload.Month);
        Assert.Equal(1500m, februaryPayload.IncomeTotal);
        Assert.Equal(900m, februaryPayload.ExpenseTotal);
        Assert.Equal(600m, februaryPayload.NetTotal);
        Assert.Equal(2, februaryPayload.TransactionCount);
    }
}
