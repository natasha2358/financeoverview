using FinanceOverview.Api.Data;
using FinanceOverview.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FinanceOverview.Api.Services;

public class MerchantRuleService
{
    private readonly AppDbContext _dbContext;

    public MerchantRuleService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MerchantRule?> FindBestMatchAsync(string rawDescription, CancellationToken cancellationToken)
    {
        var rules = await GetOrderedRulesAsync(cancellationToken);

        return FindBestMatch(rawDescription, rules);
    }

    public async Task<IReadOnlyList<MerchantRule>> GetOrderedRulesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.MerchantRules
            .AsNoTracking()
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task ApplyRulesAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken)
    {
        var transactionList = transactions.ToList();
        if (transactionList.Count == 0)
        {
            return;
        }

        var rules = await GetOrderedRulesAsync(cancellationToken);

        foreach (var transaction in transactionList)
        {
            var match = FindBestMatch(transaction.RawDescription, rules);
            if (match is null)
            {
                continue;
            }

            transaction.MerchantNormalized = match.NormalizedMerchant.Trim();
            if (match.CategoryId.HasValue)
            {
                transaction.CategoryId = match.CategoryId;
            }
        }
    }

    public static MerchantRule? FindBestMatch(string rawDescription, IReadOnlyList<MerchantRule> rules)
    {
        if (string.IsNullOrWhiteSpace(rawDescription) || rules.Count == 0)
        {
            return null;
        }

        var description = NormalizeForMatch(rawDescription);

        return rules
            .Where(rule =>
                !string.IsNullOrWhiteSpace(rule.Pattern)
                && description.Contains(NormalizeForMatch(rule.Pattern), StringComparison.OrdinalIgnoreCase)
                && rule.MatchType == MerchantRuleMatchType.Contains)
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Id)
            .FirstOrDefault();
    }

    private static string NormalizeForMatch(string value)
    {
        return string.Join(
            " ",
            value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
