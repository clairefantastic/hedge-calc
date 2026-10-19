using HedgeCalc.Api.Contracts;
using HedgeCalc.Api.Data;
using HedgeCalc.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace HedgeCalc.Api.Services;

public interface IHedgeService
{
    Task<CalculateHedgesResponse> CalculateAndSaveAsync(CalculateHedgesRequest request, CancellationToken ct);
    Task<CalculateHedgesResponse?> GetAsync(Guid id, CancellationToken ct);
}

/// <summary>
/// Application service: maps the request to domain inputs, runs the calculator,
/// persists inputs + results, and maps back to the response contract.
/// </summary>
public sealed class HedgeService(
    HedgeDbContext db,
    IHedgeCalculator calculator,
    TimeProvider clock,
    ILogger<HedgeService> logger) : IHedgeService
{
    public async Task<CalculateHedgesResponse> CalculateAndSaveAsync(
        CalculateHedgesRequest request, CancellationToken ct)
    {
        // Model validation has already run, so Exposure/HedgeRatio are non-null here.
        var inputs = request.Exposures
            .Select(e => new HedgeInput(e.Currency, e.Exposure!.Value, e.HedgeRatio!.Value))
            .ToList();

        var results = calculator.Calculate(inputs);

        var calculation = new HedgeCalculation
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = clock.GetUtcNow().UtcDateTime,
            BaseCurrency = request.BaseCurrency.ToUpperInvariant(),
            Lines = results.Select(r => new HedgeCalculationLine
            {
                Currency = r.Currency,
                Exposure = r.Exposure,
                HedgeRatio = r.HedgeRatio,
                TargetNotional = r.TargetNotional,
                Direction = r.Direction,
            }).ToList(),
        };

        db.Calculations.Add(calculation);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Saved hedge calculation {CalculationId} ({BaseCurrency}) with {LineCount} lines",
            calculation.Id, calculation.BaseCurrency, calculation.Lines.Count);

        return ToResponse(calculation);
    }

    public async Task<CalculateHedgesResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var calculation = await db.Calculations
            .AsNoTracking()
            .Include(c => c.Lines)
            .SingleOrDefaultAsync(c => c.Id == id, ct);

        return calculation is null ? null : ToResponse(calculation);
    }

    private static CalculateHedgesResponse ToResponse(HedgeCalculation calculation) =>
        new(
            calculation.Id,
            calculation.CreatedAtUtc,
            calculation.BaseCurrency,
            calculation.Lines
                .OrderBy(l => l.Id)
                .Select(l => new HedgeLineResponse(
                    l.Currency, l.Exposure, l.HedgeRatio, l.TargetNotional, l.Direction))
                .ToList());
}
