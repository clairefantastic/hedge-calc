using HedgeCalc.Api.Domain;

namespace HedgeCalc.Tests;

public class HedgeCalculatorTests
{
    private readonly HedgeCalculator _calculator = new();

    [Fact]
    public void Calculates_target_notional_as_exposure_times_ratio()
    {
        var result = HedgeCalculator.CalculateOne(new HedgeInput("EUR", 1_000_000.00m, 0.75m));

        Assert.Equal(750_000.00m, result.TargetNotional);
        Assert.Equal(HedgeDirection.SellForeign, result.Direction);
    }

    // decimal is not a valid C# attribute argument, so [InlineData(1000.01m, ...)] does not compile.
    // TheoryData<...> is the type-safe way to feed decimal cases into a [Theory].
    public static TheoryData<string, decimal, decimal, decimal> RoundingCases => new()
    {
        { "EUR", 1000.01m, 0.5m, 500.01m },     // 500.005  -> midpoint rounds away from zero (banker's would give 500.00)
        { "EUR", -1000.01m, 0.5m, -500.01m },   // -500.005 -> symmetric for negatives
        { "GBP", 333.33m, 0.333m, 111.00m },    // 110.99889 -> 111.00
        { "USD", 0.01m, 0.25m, 0.00m },         // 0.0025 -> rounds to zero
        { "JPY", 123_457m, 0.5m, 61_729m },     // 61728.5 -> JPY has 0 minor units
        { "KWD", 1000.0005m, 1m, 1000.001m },   // KWD has 3 minor units
    };

    [Theory]
    [MemberData(nameof(RoundingCases))]
    public void Rounds_to_currency_minor_units_with_midpoint_away_from_zero(
        string currency, decimal exposure, decimal ratio, decimal expected)
    {
        var result = HedgeCalculator.CalculateOne(new HedgeInput(currency, exposure, ratio));

        Assert.Equal(expected, result.TargetNotional);
    }

    [Fact]
    public void Zero_exposure_needs_no_hedge()
    {
        var result = HedgeCalculator.CalculateOne(new HedgeInput("EUR", 0m, 0.75m));

        Assert.Equal(0m, result.TargetNotional);
        Assert.Equal(HedgeDirection.None, result.Direction);
    }

    [Fact]
    public void Negative_exposure_is_a_short_position_hedged_by_buying_forward()
    {
        var result = HedgeCalculator.CalculateOne(new HedgeInput("GBP", -1_000_000m, 0.75m));

        Assert.Equal(-750_000m, result.TargetNotional);
        Assert.Equal(HedgeDirection.BuyForeign, result.Direction);
    }

    [Fact]
    public void Tiny_exposure_that_rounds_to_zero_has_no_direction()
    {
        var result = HedgeCalculator.CalculateOne(new HedgeInput("USD", -0.001m, 1m));

        Assert.Equal(0m, result.TargetNotional);
        Assert.Equal(HedgeDirection.None, result.Direction);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Ratio_bounds_are_inclusive(int ratio)
    {
        var result = HedgeCalculator.CalculateOne(new HedgeInput("EUR", 1234.56m, ratio));

        Assert.Equal(1234.56m * ratio, result.TargetNotional);
    }

    [Theory]
    [MemberData(nameof(InvalidRatios))]
    public void Ratio_outside_zero_to_one_throws(decimal ratio)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => HedgeCalculator.CalculateOne(new HedgeInput("EUR", 100m, ratio)));
    }

    public static TheoryData<decimal> InvalidRatios => new() { -0.01m, 1.0001m };

    [Fact]
    public void Calculate_preserves_order_and_normalises_currency_codes()
    {
        var results = _calculator.Calculate(new[]
        {
            new HedgeInput("jpy", 1000m, 1m),
            new HedgeInput("Eur", 1000m, 0.5m),
        });

        Assert.Collection(results,
            r => Assert.Equal("JPY", r.Currency),
            r => Assert.Equal("EUR", r.Currency));
    }

    [Fact]
    public void Decimal_is_exact_where_double_is_not()
    {
        // Documents *why* money uses decimal: base-10 arithmetic, no binary floating-point drift.
        Assert.Equal(0.3m, 0.1m + 0.2m);
        Assert.NotEqual(0.3, 0.1 + 0.2);
    }
}
