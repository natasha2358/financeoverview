using FinanceOverview.Api.Data;
using FinanceOverview.Api.Models;
using FinanceOverview.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FinanceOverview.Api.Tests;

public class MerchantRuleServiceTests
{
    [Fact]
    public async Task FindBestMatchAsync_UsesCaseInsensitiveContainsAndPriority()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var configuration = new ConfigurationBuilder().Build();
        await using var dbContext = new AppDbContext(options, configuration);
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.MerchantRules.AddRange(
            new MerchantRule
            {
                Pattern = "coffee",
                MatchType = MerchantRuleMatchType.Contains,
                NormalizedMerchant = "Coffee Shop",
                Priority = 200,
                CreatedAtUtc = DateTime.UtcNow
            },
            new MerchantRule
            {
                Pattern = "COFF",
                MatchType = MerchantRuleMatchType.Contains,
                NormalizedMerchant = "Cafe",
                Priority = 50,
                CreatedAtUtc = DateTime.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var service = new MerchantRuleService(dbContext);

        var match = await service.FindBestMatchAsync("Card payment at Coffee shop", CancellationToken.None);

        Assert.NotNull(match);
        Assert.Equal("Cafe", match!.NormalizedMerchant);
    }

    [Fact]
    public async Task FindBestMatchAsync_NormalizesWhitespaceInDescription()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var configuration = new ConfigurationBuilder().Build();
        await using var dbContext = new AppDbContext(options, configuration);
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.MerchantRules.Add(new MerchantRule
        {
            Pattern = "Coffee Shop",
            MatchType = MerchantRuleMatchType.Contains,
            NormalizedMerchant = "Coffee Shop",
            Priority = 100,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new MerchantRuleService(dbContext);

        var match = await service.FindBestMatchAsync("  Coffee   Shop   Main ", CancellationToken.None);

        Assert.NotNull(match);
        Assert.Equal("Coffee Shop", match!.NormalizedMerchant);
    }
}
