using FinanceOverview.Api.Models;

namespace FinanceOverview.Api.Services;

public interface IStatementParser
{
    string ParserKey { get; }
    bool CanParse(string extractedText);
    IReadOnlyList<StagedTransaction> Parse(int importBatchId, string extractedText);
}
