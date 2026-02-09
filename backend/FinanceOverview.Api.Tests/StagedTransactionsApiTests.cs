using System.Net;
using System.Net.Http.Json;
using System.Linq;
using FinanceOverview.Api.Dtos;
using Xunit;

namespace FinanceOverview.Api.Tests;

public class StagedTransactionsApiTests
{
    [Fact]
    public async Task PostParseToStaging_CreatesExpectedRows()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var content = StatementImportsApiTests.BuildMultipart("statement.pdf", "application/pdf");

        using var createResponse = await client.PostAsync("/api/imports", content);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(created);

        using var extractResponse = await client.PostAsync($"/api/imports/{created.Id}/extract-text", null);
        Assert.Equal(HttpStatusCode.OK, extractResponse.StatusCode);

        using var parseResponse = await client.PostAsync($"/api/imports/{created.Id}/parse-to-staging", null);
        Assert.Equal(HttpStatusCode.OK, parseResponse.StatusCode);

        var stagedRows = await parseResponse.Content.ReadFromJsonAsync<List<StagedTransactionDto>>();
        Assert.NotNull(stagedRows);
        Assert.Equal(3, stagedRows.Count);

        var first = stagedRows[0];
        Assert.Equal(new DateOnly(2026, 2, 1), first.BookingDate);
        Assert.Equal(new DateOnly(2026, 2, 1), first.ValueDate);
        Assert.Equal("Kartenzahlung COFFEE SHOP", first.RawDescription);
        Assert.Equal(-4.50m, first.Amount);
        Assert.Equal("EUR", first.Currency);
        Assert.Equal(1234.56m, first.RunningBalance);
        Assert.False(first.IsApproved);
    }

    [Fact]
    public async Task PostParseToStaging_IsIdempotent()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var content = StatementImportsApiTests.BuildMultipart("statement.pdf", "application/pdf");

        using var createResponse = await client.PostAsync("/api/imports", content);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(created);

        using var extractResponse = await client.PostAsync($"/api/imports/{created.Id}/extract-text", null);
        Assert.Equal(HttpStatusCode.OK, extractResponse.StatusCode);

        using var firstParse = await client.PostAsync($"/api/imports/{created.Id}/parse-to-staging", null);
        Assert.Equal(HttpStatusCode.OK, firstParse.StatusCode);

        using var secondParse = await client.PostAsync($"/api/imports/{created.Id}/parse-to-staging", null);
        Assert.Equal(HttpStatusCode.OK, secondParse.StatusCode);

        var stagedRows = await client.GetFromJsonAsync<List<StagedTransactionDto>>(
            $"/api/imports/{created.Id}/staged-transactions");
        Assert.NotNull(stagedRows);
        Assert.Equal(3, stagedRows.Count);
        Assert.Equal(new[] { 1, 2, 3 }, stagedRows.Select(row => row.RowIndex).ToArray());
    }
}
