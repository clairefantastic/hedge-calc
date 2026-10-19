using HedgeCalc.Api.Contracts;
using HedgeCalc.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HedgeCalc.Api.Controllers;

[ApiController]          // automatic 400 ValidationProblemDetails when the model is invalid
[Route("hedges")]
public sealed class HedgesController(IHedgeService hedgeService) : ControllerBase
{
    /// <summary>Calculates target hedge notionals per currency and stores the calculation.</summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(CalculateHedgesResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CalculateHedgesResponse>> Calculate(
        CalculateHedgesRequest request, CancellationToken ct)
    {
        var response = await hedgeService.CalculateAndSaveAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.CalculationId }, response);
    }

    /// <summary>Returns a previously stored calculation.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CalculateHedgesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalculateHedgesResponse>> GetById(Guid id, CancellationToken ct)
    {
        var response = await hedgeService.GetAsync(id, ct);
        return response is null ? NotFound() : Ok(response);
    }
}
