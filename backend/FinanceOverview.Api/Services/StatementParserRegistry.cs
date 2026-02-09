namespace FinanceOverview.Api.Services;

public interface IStatementParserRegistry
{
    IStatementParser? GetByKey(string parserKey);
}

public sealed class StatementParserRegistry : IStatementParserRegistry
{
    private readonly IReadOnlyDictionary<string, IStatementParser> _parsers;

    public StatementParserRegistry(IEnumerable<IStatementParser> parsers)
    {
        _parsers = parsers.ToDictionary(parser => parser.ParserKey, StringComparer.OrdinalIgnoreCase);
    }

    public IStatementParser? GetByKey(string parserKey)
    {
        if (string.IsNullOrWhiteSpace(parserKey))
        {
            return null;
        }

        return _parsers.TryGetValue(parserKey, out var parser)
            ? parser
            : null;
    }
}
