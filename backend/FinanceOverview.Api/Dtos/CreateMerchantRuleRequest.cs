using System.ComponentModel.DataAnnotations;

namespace FinanceOverview.Api.Dtos;

public sealed class CreateMerchantRuleRequest : IValidatableObject
{
    [Required]
    public string Pattern { get; init; } = string.Empty;

    [Required]
    public string MatchType { get; init; } = "Contains";

    [Required]
    public string NormalizedMerchant { get; init; } = string.Empty;

    public int? CategoryId { get; init; }

    public int? Priority { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Pattern))
        {
            yield return new ValidationResult("Pattern is required.", new[] { nameof(Pattern) });
        }

        if (string.IsNullOrWhiteSpace(NormalizedMerchant))
        {
            yield return new ValidationResult("Normalized merchant is required.", new[] { nameof(NormalizedMerchant) });
        }

        if (string.IsNullOrWhiteSpace(MatchType))
        {
            yield return new ValidationResult("Match type is required.", new[] { nameof(MatchType) });
        }
    }
}
