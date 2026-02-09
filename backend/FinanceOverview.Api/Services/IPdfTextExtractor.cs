namespace FinanceOverview.Api.Services;

public interface IPdfTextExtractor
{
    Task<string> ExtractTextAsync(string pdfPath, CancellationToken cancellationToken);
}
