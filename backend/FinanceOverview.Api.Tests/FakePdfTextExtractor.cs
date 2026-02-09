using FinanceOverview.Api.Services;

namespace FinanceOverview.Api.Tests;

public sealed class FakePdfTextExtractor : IPdfTextExtractor
{
    public const string ExtractedText = """
        01/02/2026 Grocery Store -12.34
        03/02/2026 Rent Payment -900.00
        """;

    public Task<string> ExtractTextAsync(string pdfPath, CancellationToken cancellationToken)
    {
        return Task.FromResult(ExtractedText);
    }
}
