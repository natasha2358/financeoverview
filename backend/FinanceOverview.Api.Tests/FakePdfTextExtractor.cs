using FinanceOverview.Api.Services;

namespace FinanceOverview.Api.Tests;

public sealed class FakePdfTextExtractor : IPdfTextExtractor
{
    public const string ExtractedText = """
HASPA Kontoauszug
Hamburger Sparkasse
Buchungstag Wertstellung Buchungstext Betrag Waehrung Saldo
01.02.2026 01.02.2026 Kartenzahlung COFFEE SHOP -4,50 EUR 1.234,56
02.02.2026 03.02.2026 Gehalt ACME CORP 2.500,00 EUR 3.734,56
03.02.2026 03.02.2026 Dauerauftrag RENT -800,00 EUR 2.934,56
""";

    public Task<string> ExtractTextAsync(string pdfPath, CancellationToken cancellationToken)
    {
        return Task.FromResult(ExtractedText);
    }
}
