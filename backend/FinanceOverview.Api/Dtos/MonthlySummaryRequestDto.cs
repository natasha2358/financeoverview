using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace FinanceOverview.Api.Dtos;

public sealed class MonthlySummaryRequestDto : IValidatableObject
{
    [Required]
    public string Month { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Month))
        {
            yield return new ValidationResult("Month is required.", new[] { nameof(Month) });
            yield break;
        }

        if (!DateOnly.TryParseExact(Month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            yield return new ValidationResult("Month must be in YYYY-MM format.", new[] { nameof(Month) });
        }
    }
}
