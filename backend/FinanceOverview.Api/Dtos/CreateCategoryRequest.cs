using System.ComponentModel.DataAnnotations;

namespace FinanceOverview.Api.Dtos;

public sealed class CreateCategoryRequest : IValidatableObject
{
    [Required]
    public string Name { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Category name is required.", new[] { nameof(Name) });
        }
    }
}
