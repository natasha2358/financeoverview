using FinanceOverview.Api.Services;

namespace FinanceOverview.Api.Tests;

public sealed class FakePdfTextExtractor : IPdfTextExtractor
{
    public const string ExtractedText = "Synthetic extracted text for testing.";

    public Task<string> ExtractTextAsync(string pdfPath, CancellationToken cancellationToken)
    {
        return Task.FromResult(ExtractedText);
    }
}
