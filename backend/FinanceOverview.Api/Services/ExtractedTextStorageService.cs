using Microsoft.AspNetCore.Hosting;

namespace FinanceOverview.Api.Services;

public sealed class ExtractedTextStorageService
{
    private const string ExtractedFolderName = "extracted";
    private readonly string _extractedRoot;

    public ExtractedTextStorageService(IWebHostEnvironment environment)
    {
        _extractedRoot = Path.Combine(environment.ContentRootPath, "App_Data", ExtractedFolderName);
        Directory.CreateDirectory(_extractedRoot);
    }

    public string GetExtractedTextPath(int importBatchId)
    {
        return Path.Combine(_extractedRoot, $"{importBatchId}.txt");
    }

    public Task SaveExtractedTextAsync(int importBatchId, string text, CancellationToken cancellationToken)
    {
        var path = GetExtractedTextPath(importBatchId);
        return File.WriteAllTextAsync(path, text ?? string.Empty, cancellationToken);
    }
}
