using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceOverview.Api.Dtos;
using Xunit;
using System.Linq;

namespace FinanceOverview.Api.Tests;

public class StatementImportsApiTests
{
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

    private static MultipartFormDataContent BuildMultipart(string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("%PDF-1.4 test".Select(c => (byte)c).ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "pdf", fileName);
        content.Add(new StringContent("2026-02"), "statementMonth");
        return content;
    }
}
