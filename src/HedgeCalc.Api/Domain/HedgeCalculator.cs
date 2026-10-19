namespace HedgeCalc.Api.Domain;

/// <summary>
/// Which way the forward contract goes, from the base-currency investor's view.
/// Long foreign exposure (positive) is hedged by selling the foreign currency forward;
/// short foreign exposure (negative) is hedged by buying it forward.
/// </summary>
public enum HedgeDirection
{
    None,
    SellForeign,
    BuyForeign,
}

public sealed record HedgeInput(string Currency, decimal Exposure, decimal HedgeRatio);

public sealed record HedgeResult(
    string Currency,
    decimal Exposure,
    decimal HedgeRatio,
    decimal TargetNotional,
    HedgeDirection Direction);

public interface IHedgeCalculator
{
    IReadOnlyList<HedgeResult> Calculate(IEnumerable<HedgeInput> inputs);
}

/// <summary>
/// Pure calculation logic: no database, no HTTP, no clock. That keeps it trivially unit-testable.
/// TargetNotional = Exposure x HedgeRatio, rounded to the currency's minor units,
/// with midpoints rounded away from zero (500.005 -> 500.01, -500.005 -> -500.01).
/// </summary>
public sealed class HedgeCalculator : IHedgeCalculator
{
    public IReadOnlyList<HedgeResult> Calculate(IEnumerable<HedgeInput> inputs) =>
        inputs.Select(CalculateOne).ToList();

    public static HedgeResult CalculateOne(HedgeInput input)
    {
        if (input.HedgeRatio < 0m || input.HedgeRatio > 1m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input), input.HedgeRatio, "Hedge ratio must be between 0 and 1.");
        }

        var currency = input.Currency.ToUpperInvariant();
        var decimals = CurrencyRules.MinorUnits(currency);

        var rawNotional = input.Exposure * input.HedgeRatio;
        var target = Math.Round(rawNotional, decimals, MidpointRounding.AwayFromZero);

        var direction = target switch
        {
            > 0m => HedgeDirection.SellForeign,
            < 0m => HedgeDirection.BuyForeign,
            _ => HedgeDirection.None,
        };

        return new HedgeResult(currency, input.Exposure, input.HedgeRatio, target, direction);
    }
}
