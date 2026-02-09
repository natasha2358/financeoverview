using System.Text;
using UglyToad.PdfPig;

namespace FinanceOverview.Api.Services;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public Task<string> ExtractTextAsync(string pdfPath, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            using var document = PdfDocument.Open(pdfPath);
            var builder = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.AppendLine(page.Text);
            }

            return builder.ToString().TrimEnd();
        }, cancellationToken);
    }
}
