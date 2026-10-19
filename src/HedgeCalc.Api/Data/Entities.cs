using HedgeCalc.Api.Domain;

namespace HedgeCalc.Api.Data;

/// <summary>One call to POST /hedges/calculate (the "header" row).</summary>
public class HedgeCalculation
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string BaseCurrency { get; set; } = string.Empty;
    public List<HedgeCalculationLine> Lines { get; set; } = new();
}

/// <summary>One currency inside a calculation: the input and the result, stored together.</summary>
public class HedgeCalculationLine
{
    public int Id { get; set; }
    public Guid HedgeCalculationId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal Exposure { get; set; }
    public decimal HedgeRatio { get; set; }
    public decimal TargetNotional { get; set; }
    public HedgeDirection Direction { get; set; }
}
