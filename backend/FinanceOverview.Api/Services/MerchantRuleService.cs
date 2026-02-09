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
        var rules = await _dbContext.MerchantRules
            .AsNoTracking()
            .OrderBy(rule => rule.Priority)
            .ToListAsync(cancellationToken);

        return FindBestMatch(rawDescription, rules);
    }

    public async Task ApplyRulesAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken)
    {
        var transactionList = transactions.ToList();
        if (transactionList.Count == 0)
        {
            return;
        }

        var rules = await _dbContext.MerchantRules
            .AsNoTracking()
            .OrderBy(rule => rule.Priority)
            .ToListAsync(cancellationToken);

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

    private static MerchantRule? FindBestMatch(string rawDescription, IReadOnlyList<MerchantRule> rules)
    {
        if (string.IsNullOrWhiteSpace(rawDescription) || rules.Count == 0)
        {
            return null;
        }

        var description = rawDescription.Trim();

        return rules
            .Where(rule =>
                !string.IsNullOrWhiteSpace(rule.Pattern)
                && description.Contains(rule.Pattern.Trim(), StringComparison.OrdinalIgnoreCase)
                && rule.MatchType == MerchantRuleMatchType.Contains)
            .OrderBy(rule => rule.Priority)
            .FirstOrDefault();
    }
}
