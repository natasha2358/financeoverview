namespace FinanceOverview.Api.Dtos;

public sealed record MerchantRuleDto(
    int Id,
    string Pattern,
    string MatchType,
    string NormalizedMerchant,
    int? CategoryId,
    string? CategoryName,
    int Priority,
    DateTime CreatedAtUtc);
