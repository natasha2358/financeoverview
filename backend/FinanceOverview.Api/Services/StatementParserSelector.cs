using FinanceOverview.Api.Models;

namespace FinanceOverview.Api.Services;

public interface IStatementParserSelector
{
    string SelectParserKey(ImportBatch importBatch, string extractedText);
}

public sealed class DefaultStatementParserSelector : IStatementParserSelector
{
    public const string UnknownParserKey = "unknown";

    public string SelectParserKey(ImportBatch importBatch, string extractedText)
    {
        if (SparkasseHaspaV1ParserDetects(extractedText))
        {
            return SparkasseHaspaV1Parser.ParserKeyValue;
        }

        return UnknownParserKey;
    }

    private static bool SparkasseHaspaV1ParserDetects(string extractedText)
    {
        return extractedText.Contains("HASPA", StringComparison.OrdinalIgnoreCase)
            || extractedText.Contains("Sparkasse", StringComparison.OrdinalIgnoreCase)
            || extractedText.Contains("Hamburger Sparkasse", StringComparison.OrdinalIgnoreCase);
    }
}
