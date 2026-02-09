namespace FinanceOverview.Api.Models;

public class MerchantRule
{
    public int Id { get; set; }
    public string Pattern { get; set; } = string.Empty;
    public MerchantRuleMatchType MatchType { get; set; }
    public string NormalizedMerchant { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int Priority { get; set; } = 100;
    public DateTime CreatedAtUtc { get; set; }
}
