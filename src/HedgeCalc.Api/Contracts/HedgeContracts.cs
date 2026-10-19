using System.ComponentModel.DataAnnotations;
using HedgeCalc.Api.Domain;

namespace HedgeCalc.Api.Contracts;

/// <summary>Request body for POST /hedges/calculate.</summary>
public sealed class CalculateHedgesRequest : IValidatableObject
{
    [Required]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "BaseCurrency must be a 3-letter ISO 4217 code.")]
    public string BaseCurrency { get; init; } = "USD";

    [Required]
    [MinLength(1, ErrorMessage = "At least one exposure is required.")]
    public List<ExposureLine> Exposures { get; init; } = new();

    // Cross-field rules that a single attribute cannot express.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Exposures is null)
        {
            yield break;
        }

        var duplicates = Exposures
            .Where(e => e is not null && !string.IsNullOrWhiteSpace(e.Currency))
            .GroupBy(e => e.Currency.ToUpperInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            yield return new ValidationResult(
                $"Duplicate currencies: {string.Join(", ", duplicates)}.",
                new[] { nameof(Exposures) });
        }

        if (Exposures.Any(e => e is not null &&
                               string.Equals(e.Currency, BaseCurrency, StringComparison.OrdinalIgnoreCase)))
        {
            yield return new ValidationResult(
                "An exposure currency cannot be the same as the base currency.",
                new[] { nameof(Exposures) });
        }
    }
}

public sealed class ExposureLine
{
    [Required]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Currency must be a 3-letter ISO 4217 code.")]
    public string Currency { get; init; } = string.Empty;

    // decimal? (nullable) so that a missing field is a 400 error instead of silently becoming 0.
    // Negative values are allowed: they represent a short foreign-currency position.
    [Required]
    public decimal? Exposure { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "1", ErrorMessage = "HedgeRatio must be between 0 and 1.")]
    public decimal? HedgeRatio { get; init; }
}

public sealed record HedgeLineResponse(
    string Currency,
    decimal Exposure,
    decimal HedgeRatio,
    decimal TargetNotional,
    HedgeDirection Direction);

public sealed record CalculateHedgesResponse(
    Guid CalculationId,
    DateTime CreatedAtUtc,
    string BaseCurrency,
    IReadOnlyList<HedgeLineResponse> Hedges);
