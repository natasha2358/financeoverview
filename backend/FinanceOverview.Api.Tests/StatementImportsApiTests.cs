using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Linq;

namespace FinanceOverview.Api.Tests;

public class StatementImportsApiTests
{
    [Fact]
    public async Task PostExtractText_UpdatesMetadata()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var content = BuildMultipart("statement.pdf", "application/pdf");

        using var createResponse = await client.PostAsync("/api/imports", content);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(created);

        using var extractResponse = await client.PostAsync($"/api/imports/{created.Id}/extract-text", null);
        Assert.Equal(HttpStatusCode.OK, extractResponse.StatusCode);

        var extractedPayload = await extractResponse.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(extractedPayload);
        Assert.Equal("Parsed", extractedPayload.Status);
        Assert.NotNull(extractedPayload.ExtractedAtUtc);
        Assert.Equal(2, extractedPayload.ParsedRowCount);
        Assert.Equal(new DateOnly(2026, 2, 1), extractedPayload.FirstBookingDate);
        Assert.Equal(new DateOnly(2026, 2, 3), extractedPayload.LastBookingDate);

        var refreshed = await client.GetFromJsonAsync<ImportBatchDto>($"/api/imports/{created.Id}");
        Assert.NotNull(refreshed);
        Assert.Equal("Parsed", refreshed.Status);
        Assert.NotNull(refreshed.ExtractedAtUtc);
        Assert.Equal(2, refreshed.ParsedRowCount);
        Assert.Equal(new DateOnly(2026, 2, 1), refreshed.FirstBookingDate);
        Assert.Equal(new DateOnly(2026, 2, 3), refreshed.LastBookingDate);
    }

    [Fact]
    public async Task GetExtractedText_ReturnsContentAfterExtraction()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var content = BuildMultipart("statement.pdf", "application/pdf");

        using var createResponse = await client.PostAsync("/api/imports", content);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(created);

        using var extractResponse = await client.PostAsync($"/api/imports/{created.Id}/extract-text", null);
        Assert.Equal(HttpStatusCode.OK, extractResponse.StatusCode);

        using var extractedTextResponse =
            await client.GetAsync($"/api/imports/{created.Id}/extracted-text");
        Assert.Equal(HttpStatusCode.OK, extractedTextResponse.StatusCode);

        var text = await extractedTextResponse.Content.ReadAsStringAsync();
        Assert.Equal(FakePdfTextExtractor.ExtractedText, text);
    }

    [Fact]
    public async Task PostExtractText_CanBeCalledTwice()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var content = BuildMultipart("statement.pdf", "application/pdf");

        using var createResponse = await client.PostAsync("/api/imports", content);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(created);

        using var firstExtract = await client.PostAsync($"/api/imports/{created.Id}/extract-text", null);
        Assert.Equal(HttpStatusCode.OK, firstExtract.StatusCode);

        using var secondExtract = await client.PostAsync($"/api/imports/{created.Id}/extract-text", null);
        Assert.Equal(HttpStatusCode.OK, secondExtract.StatusCode);

        var extractedPayload = await secondExtract.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(extractedPayload);
        Assert.Equal("Parsed", extractedPayload.Status);
        Assert.Equal(2, extractedPayload.ParsedRowCount);
        Assert.Equal(new DateOnly(2026, 2, 1), extractedPayload.FirstBookingDate);
        Assert.Equal(new DateOnly(2026, 2, 3), extractedPayload.LastBookingDate);

        using var extractedTextResponse =
            await client.GetAsync($"/api/imports/{created.Id}/extracted-text");
        Assert.Equal(HttpStatusCode.OK, extractedTextResponse.StatusCode);

        var text = await extractedTextResponse.Content.ReadAsStringAsync();
        Assert.Equal(FakePdfTextExtractor.ExtractedText, text);
    }

    [Fact]
    public async Task PostExtractText_DoesNotDowngradeStatus()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var content = BuildMultipart("statement.pdf", "application/pdf");

        using var createResponse = await client.PostAsync("/api/imports", content);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(created);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var import = await dbContext.ImportBatches.FindAsync(created.Id);
            Assert.NotNull(import);
            import.Status = ImportBatchStatus.Parsed;
            await dbContext.SaveChangesAsync();
        }

        using var extractResponse = await client.PostAsync($"/api/imports/{created.Id}/extract-text", null);
        Assert.Equal(HttpStatusCode.OK, extractResponse.StatusCode);

        var extractedPayload = await extractResponse.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(extractedPayload);
        Assert.Equal("Parsed", extractedPayload.Status);
        Assert.NotNull(extractedPayload.ExtractedAtUtc);
        Assert.Equal(2, extractedPayload.ParsedRowCount);
        Assert.Equal(new DateOnly(2026, 2, 1), extractedPayload.FirstBookingDate);
        Assert.Equal(new DateOnly(2026, 2, 3), extractedPayload.LastBookingDate);
    }

    [Fact]
    public async Task PostPdf_CreatesImportBatch()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var content = BuildMultipart("statement.pdf", "application/pdf");

        using var response = await client.PostAsync("/api/imports", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var payload = await response.Content.ReadFromJsonAsync<ImportBatchDto>();

        Assert.NotNull(payload);
        Assert.True(payload.Id > 0);
        Assert.Equal("statement.pdf", payload.OriginalFileName);
        Assert.Equal("Uploaded", payload.Status);
    }

    [Fact]
    public async Task PostNonPdf_ReturnsBadRequest()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var content = BuildMultipart("statement.txt", "text/plain");

        using var response = await client.PostAsync("/api/imports", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_ReturnsLatestImports()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var content = BuildMultipart("statement.pdf", "application/pdf");

        using var createResponse = await client.PostAsync("/api/imports", content);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var list = await client.GetFromJsonAsync<List<ImportBatchDto>>("/api/imports");

        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal("statement.pdf", list[0].OriginalFileName);
    }

    [Fact]
    public async Task PostPdf_DuplicateUpload_ReturnsExistingBatch()
    {
        using var factory = new TestWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        using var content = BuildMultipart("statement.pdf", "application/pdf");
        using var duplicateContent = BuildMultipart("statement.pdf", "application/pdf");

        using var firstResponse = await client.PostAsync("/api/imports", content);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(firstPayload);

        using var secondResponse = await client.PostAsync("/api/imports", duplicateContent);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<ImportBatchDto>();
        Assert.NotNull(secondPayload);
        Assert.Equal(firstPayload.Id, secondPayload.Id);

        var list = await client.GetFromJsonAsync<List<ImportBatchDto>>("/api/imports");
        Assert.NotNull(list);
        Assert.Single(list);
    }

    public static MultipartFormDataContent BuildMultipart(string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("%PDF-1.4 test".Select(c => (byte)c).ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "pdf", fileName);
        content.Add(new StringContent("2026-02"), "statementMonth");
        return content;
    }
}
