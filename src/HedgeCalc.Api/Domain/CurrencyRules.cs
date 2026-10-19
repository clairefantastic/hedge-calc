namespace HedgeCalc.Api.Domain;

/// <summary>
/// Currency-specific rules. Most currencies have 2 minor units (cents),
/// but ISO 4217 defines exceptions such as JPY (0) and KWD (3).
/// </summary>
public static class CurrencyRules
{
    private const int DefaultMinorUnits = 2;

    private static readonly IReadOnlyDictionary<string, int> MinorUnitOverrides =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["JPY"] = 0,
            ["KRW"] = 0,
            ["CLP"] = 0,
            ["ISK"] = 0,
            ["BHD"] = 3,
            ["JOD"] = 3,
            ["KWD"] = 3,
            ["OMR"] = 3,
            ["TND"] = 3,
        };

    public static int MinorUnits(string currency) =>
        MinorUnitOverrides.TryGetValue(currency, out var units) ? units : DefaultMinorUnits;
}
