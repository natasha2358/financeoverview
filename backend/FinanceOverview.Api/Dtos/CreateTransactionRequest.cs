using System.ComponentModel.DataAnnotations;

namespace FinanceOverview.Api.Dtos;

public sealed class CreateTransactionRequest : IValidatableObject
{
    [Required]
    public DateOnly Date { get; init; }

    [Required]
    public string RawDescription { get; init; } = string.Empty;

    public string? Merchant { get; init; }

    [Required]
    public decimal Amount { get; init; }

    public string? Currency { get; init; }

    public decimal? Balance { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Date == default)
        {
            yield return new ValidationResult("Date is required.", new[] { nameof(Date) });
        }

        if (Amount == 0)
        {
            yield return new ValidationResult("Amount must be non-zero.", new[] { nameof(Amount) });
        }

        if (string.IsNullOrWhiteSpace(RawDescription))
        {
            yield return new ValidationResult("Raw description is required.", new[] { nameof(RawDescription) });
        }
    }
}
