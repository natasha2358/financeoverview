using System.Globalization;
using FinanceOverview.Api.Data;
using FinanceOverview.Api.Dtos;
using FinanceOverview.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinanceOverview.Api.Controllers;

[ApiController]
[Route("api/rules")]
public class RulesController : ControllerBase
{
    private const int MerchantCandidateLength = 40;
    private readonly AppDbContext _dbContext;

    public RulesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MerchantRuleDto>>> Get()
    {
        var rules = await _dbContext.MerchantRules
            .AsNoTracking()
            .Include(rule => rule.Category)
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Id)
            .Select(rule => new MerchantRuleDto(
                rule.Id,
                rule.Pattern,
                rule.MatchType.ToString(),
                rule.NormalizedMerchant,
                rule.CategoryId,
                rule.Category != null ? rule.Category.Name : null,
                rule.Priority,
                rule.CreatedAtUtc))
            .ToListAsync();

        return Ok(rules);
    }

    [HttpPost]
    public async Task<ActionResult<MerchantRuleDto>> Create([FromBody] CreateMerchantRuleRequest request)
    {
        if (!string.Equals(request.MatchType, MerchantRuleMatchType.Contains.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only MatchType 'Contains' is supported right now." });
        }

        int? categoryId = null;
        string? categoryName = null;

        if (request.CategoryId.HasValue)
        {
            var category = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(entry => entry.Id == request.CategoryId.Value);

            if (category is null)
            {
                return BadRequest(new { error = "Category does not exist." });
            }

            categoryId = category.Id;
            categoryName = category.Name;
        }

        var rule = new MerchantRule
        {
            Pattern = request.Pattern.Trim(),
            MatchType = MerchantRuleMatchType.Contains,
            NormalizedMerchant = request.NormalizedMerchant.Trim(),
            CategoryId = categoryId,
            Priority = request.Priority ?? 100,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.MerchantRules.Add(rule);
        await _dbContext.SaveChangesAsync();

        var response = new MerchantRuleDto(
            rule.Id,
            rule.Pattern,
            rule.MatchType.ToString(),
            rule.NormalizedMerchant,
            rule.CategoryId,
            categoryName,
            rule.Priority,
            rule.CreatedAtUtc);

        return Created("/api/rules", response);
    }

    [HttpPost("unmapped-merchants")]
    public async Task<ActionResult<IReadOnlyList<string>>> PostUnmappedMerchants(
        [FromBody] UnmappedMerchantsRequest request)
    {
        if (!TryParseMonth(request.Month, out var monthStart))
        {
            return BadRequest(new { error = "Month must be in YYYY-MM format." });
        }

        var monthEnd = monthStart.AddMonths(1);

        var candidates = await _dbContext.Transactions
            .AsNoTracking()
            .Where(transaction =>
                transaction.MerchantNormalized == null
                && transaction.Date >= monthStart
                && transaction.Date < monthEnd)
            .Select(transaction => transaction.RawDescription)
            .ToListAsync();

        var distinctCandidates = candidates
            .Select(DeriveMerchantCandidate)
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(candidate => candidate)
            .ToList();

        return Ok(distinctCandidates);
    }

    private static string DeriveMerchantCandidate(string rawDescription)
    {
        var trimmed = rawDescription.Trim();
        var collapsed = string.Join(
            " ",
            trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (collapsed.Length <= MerchantCandidateLength)
        {
            return collapsed;
        }

        return collapsed[..MerchantCandidateLength];
    }

    private static bool TryParseMonth(string value, out DateOnly monthStart)
    {
        if (DateTime.TryParseExact(
                value,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            monthStart = new DateOnly(parsed.Year, parsed.Month, 1);
            return true;
        }

        monthStart = default;
        return false;
    }
}
