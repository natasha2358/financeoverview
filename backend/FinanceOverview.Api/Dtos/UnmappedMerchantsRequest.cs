using System.ComponentModel.DataAnnotations;

namespace FinanceOverview.Api.Dtos;

public sealed class UnmappedMerchantsRequest : IValidatableObject
{
    [Required]
    public string Month { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Month))
        {
            yield return new ValidationResult("Month is required.", new[] { nameof(Month) });
        }
    }
}
