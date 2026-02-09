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
        return UnknownParserKey;
    }
}
