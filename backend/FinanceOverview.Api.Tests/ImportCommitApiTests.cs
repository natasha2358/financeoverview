using System.Net;
using System.Net.Http.Json;
using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinanceOverview.Api.Tests;

public class ImportCommitApiTests
{
    [Fact]
    public async Task PostCommit_PersistsApprovedRows()
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

        var approveRequest = new UpdateStagedTransactionApprovalRequest { IsApproved = true };
        using var approveResponse = await client.PutAsJsonAsync(
            $"/api/imports/{created.Id}/staged-transactions/{stagedRows[0].Id}/approval",
            approveRequest);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        using var secondApproveResponse = await client.PutAsJsonAsync(
            $"/api/imports/{created.Id}/staged-transactions/{stagedRows[1].Id}/approval",
            approveRequest);
        Assert.Equal(HttpStatusCode.OK, secondApproveResponse.StatusCode);

        using var commitResponse = await client.PostAsync($"/api/imports/{created.Id}/commit", null);
        Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);

        var commitPayload = await commitResponse.Content.ReadFromJsonAsync<CommitImportBatchResultDto>();
        Assert.NotNull(commitPayload);
        Assert.Equal(2, commitPayload.CommittedCount);
        Assert.Equal(2, commitPayload.ApprovedCount);
        Assert.Equal(0, commitPayload.SkippedCount);
        Assert.Equal("Committed", commitPayload.Status);

        var transactions = await client.GetFromJsonAsync<List<TransactionDto>>("/api/transactions");
        Assert.NotNull(transactions);
        Assert.Equal(2, transactions.Count);
        Assert.Equal(stagedRows[1].RawDescription, transactions[0].Merchant);
        Assert.Equal(stagedRows[1].RawDescription, transactions[0].RawDescription);
        Assert.Equal(stagedRows[1].Amount, transactions[0].Amount);
    }

    [Fact]
    public async Task PostCommit_IsIdempotent()
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

        foreach (var row in stagedRows)
        {
            var approveRequest = new UpdateStagedTransactionApprovalRequest { IsApproved = true };
            using var approveResponse = await client.PutAsJsonAsync(
                $"/api/imports/{created.Id}/staged-transactions/{row.Id}/approval",
                approveRequest);
            Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        }

        using var firstCommit = await client.PostAsync($"/api/imports/{created.Id}/commit", null);
        Assert.Equal(HttpStatusCode.OK, firstCommit.StatusCode);

        using var secondCommit = await client.PostAsync($"/api/imports/{created.Id}/commit", null);
        Assert.Equal(HttpStatusCode.OK, secondCommit.StatusCode);

        var secondPayload = await secondCommit.Content.ReadFromJsonAsync<CommitImportBatchResultDto>();
        Assert.NotNull(secondPayload);
        Assert.Equal(3, secondPayload.ApprovedCount);
        Assert.Equal(0, secondPayload.CommittedCount);
        Assert.Equal(3, secondPayload.SkippedCount);

        var transactions = await client.GetFromJsonAsync<List<TransactionDto>>("/api/transactions");
        Assert.NotNull(transactions);
        Assert.Equal(3, transactions.Count);
    }

    [Fact]
    public async Task PostCommit_AppliesMerchantRules()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.MerchantRules.Add(new MerchantRule
            {
                Pattern = "COFFEE",
                MatchType = MerchantRuleMatchType.Contains,
                NormalizedMerchant = "Coffee Shop",
                Priority = 10,
                CreatedAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

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

        var approveRequest = new UpdateStagedTransactionApprovalRequest { IsApproved = true };
        using var approveResponse = await client.PutAsJsonAsync(
            $"/api/imports/{created.Id}/staged-transactions/{stagedRows[0].Id}/approval",
            approveRequest);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        using var commitResponse = await client.PostAsync($"/api/imports/{created.Id}/commit", null);
        Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var committed = await verificationDbContext.Transactions
            .OrderByDescending(transaction => transaction.Date)
            .FirstOrDefaultAsync();

        Assert.NotNull(committed);
        Assert.Equal("Coffee Shop", committed!.MerchantNormalized);
    }
}
