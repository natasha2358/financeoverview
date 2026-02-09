using System.Globalization;
using System.Text.RegularExpressions;
using FinanceOverview.Api.Models;

namespace FinanceOverview.Api.Services;

public sealed class SparkasseHaspaV1Parser : IStatementParser
{
    public const string ParserKeyValue = "sparkasse_haspa_v1";

    private static readonly Regex LineWithValueDate = new(
        @"^(?<booking>\d{2}\.\d{2}\.\d{4})\s+(?<value>\d{2}\.\d{2}\.\d{4})\s+(?<desc>.+?)\s+(?<amount>-?\d{1,3}(?:\.\d{3})*,\d{2})\s+(?<currency>[A-Z]{3})(?:\s+(?<balance>-?\d{1,3}(?:\.\d{3})*,\d{2}))?$",
        RegexOptions.Compiled);

    private static readonly Regex LineWithoutValueDate = new(
        @"^(?<booking>\d{2}\.\d{2}\.\d{4})\s+(?<desc>.+?)\s+(?<amount>-?\d{1,3}(?:\.\d{3})*,\d{2})\s+(?<currency>[A-Z]{3})(?:\s+(?<balance>-?\d{1,3}(?:\.\d{3})*,\d{2}))?$",
        RegexOptions.Compiled);

    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public string ParserKey => ParserKeyValue;

    public bool CanParse(string extractedText)
    {
        return HasSparkasseMarker(extractedText);
    }

    public IReadOnlyList<StagedTransaction> Parse(int importBatchId, string extractedText)
    {
        var results = new List<StagedTransaction>();
        var lines = extractedText.Split(
            new[] { "\r\n", "\n" },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var rowIndex = 0;
        foreach (var line in lines)
        {
            if (TryParseLine(line, out var bookingDate, out var valueDate, out var description,
                    out var amount, out var currency, out var balance))
            {
                rowIndex++;
                results.Add(new StagedTransaction
                {
                    ImportBatchId = importBatchId,
                    RowIndex = rowIndex,
                    BookingDate = bookingDate,
                    ValueDate = valueDate,
                    RawDescription = description,
                    Amount = amount,
                    Currency = currency,
                    RunningBalance = balance,
                    IsApproved = false
                });
            }
        }

        return results;
    }

    private static bool TryParseLine(
        string line,
        out DateOnly bookingDate,
        out DateOnly? valueDate,
        out string description,
        out decimal amount,
        out string? currency,
        out decimal? balance)
    {
        var match = LineWithValueDate.Match(line);
        if (match.Success)
        {
            bookingDate = ParseDate(match.Groups["booking"].Value);
            valueDate = ParseDate(match.Groups["value"].Value);
            description = match.Groups["desc"].Value.Trim();
            amount = ParseAmount(match.Groups["amount"].Value);
            currency = match.Groups["currency"].Value;
            balance = ParseNullableAmount(match.Groups["balance"].Value);
            return true;
        }

        match = LineWithoutValueDate.Match(line);
        if (match.Success)
        {
            bookingDate = ParseDate(match.Groups["booking"].Value);
            valueDate = null;
            description = match.Groups["desc"].Value.Trim();
            amount = ParseAmount(match.Groups["amount"].Value);
            currency = match.Groups["currency"].Value;
            balance = ParseNullableAmount(match.Groups["balance"].Value);
            return true;
        }

        bookingDate = default;
        valueDate = null;
        description = string.Empty;
        amount = 0m;
        currency = null;
        balance = null;
        return false;
    }

    private static DateOnly ParseDate(string value)
    {
        return DateOnly.ParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture);
    }

    private static decimal ParseAmount(string value)
    {
        return decimal.Parse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, GermanCulture);
    }

    private static decimal? ParseNullableAmount(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : ParseAmount(value);
    }

    private static bool HasSparkasseMarker(string extractedText)
    {
        return extractedText.Contains("HASPA", StringComparison.OrdinalIgnoreCase)
            || extractedText.Contains("Sparkasse", StringComparison.OrdinalIgnoreCase)
            || extractedText.Contains("Hamburger Sparkasse", StringComparison.OrdinalIgnoreCase);
    }
}
