using System.ComponentModel.DataAnnotations;

namespace FinanceOverview.Api.Dtos;

public sealed class MonthlySummaryRequest : IValidatableObject
{
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate > EndDate)
        {
            yield return new ValidationResult(
                "StartDate must be on or before EndDate.",
                new[] { nameof(StartDate), nameof(EndDate) });
        }
    }
}
