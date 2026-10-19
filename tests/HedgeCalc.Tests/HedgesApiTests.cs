using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HedgeCalc.Api.Contracts;
using HedgeCalc.Api.Data;
using HedgeCalc.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HedgeCalc.Tests;

public class HedgesApiTests(HedgeApiFactory factory) : IClassFixture<HedgeApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_valid_request_returns_201_and_persists_inputs_and_results()
    {
        var request = new
        {
            baseCurrency = "usd",
            exposures = new[]
            {
                new { currency = "EUR", exposure = 1_000_000.00m, hedgeRatio = 0.75m },
                new { currency = "gbp", exposure = -250_000.00m, hedgeRatio = 1m },
                new { currency = "CHF", exposure = 0m, hedgeRatio = 0.5m },
            },
        };

        var response = await _client.PostAsJsonAsync("/hedges/calculate", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<CalculateHedgesResponse>(Json);
        Assert.NotNull(body);
        Assert.Equal("USD", body.BaseCurrency);
        Assert.Collection(body.Hedges,
            h => { Assert.Equal("EUR", h.Currency); Assert.Equal(750_000m, h.TargetNotional); Assert.Equal(HedgeDirection.SellForeign, h.Direction); },
            h => { Assert.Equal("GBP", h.Currency); Assert.Equal(-250_000m, h.TargetNotional); Assert.Equal(HedgeDirection.BuyForeign, h.Direction); },
            h => { Assert.Equal("CHF", h.Currency); Assert.Equal(0m, h.TargetNotional); Assert.Equal(HedgeDirection.None, h.Direction); });

        // Verify it really reached the database, not just the response.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HedgeDbContext>();
        var saved = await db.Calculations.Include(c => c.Lines).SingleAsync(c => c.Id == body.CalculationId);
        Assert.Equal(3, saved.Lines.Count);
        Assert.Contains(saved.Lines, l => l.Currency == "EUR" && l.Exposure == 1_000_000m && l.HedgeRatio == 0.75m);
    }

    [Fact]
    public async Task Get_returns_a_saved_calculation()
    {
        var post = await _client.PostAsJsonAsync("/hedges/calculate", new
        {
            baseCurrency = "USD",
            exposures = new[] { new { currency = "JPY", exposure = 123_457m, hedgeRatio = 0.5m } },
        });
        var created = await post.Content.ReadFromJsonAsync<CalculateHedgesResponse>(Json);

        var fetched = await _client.GetFromJsonAsync<CalculateHedgesResponse>(post.Headers.Location, Json);

        Assert.NotNull(created);
        Assert.NotNull(fetched);
        Assert.Equal(created.CalculationId, fetched.CalculationId);
        Assert.Equal(61_729m, fetched.Hedges.Single().TargetNotional);
    }

    [Fact]
    public async Task Amounts_sent_as_strings_are_accepted()
    {
        var response = await _client.PostAsJsonAsync("/hedges/calculate", new
        {
            baseCurrency = "USD",
            exposures = new[] { new { currency = "EUR", exposure = "1000.01", hedgeRatio = "0.5" } },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CalculateHedgesResponse>(Json);
        Assert.Equal(500.01m, body!.Hedges.Single().TargetNotional);
    }

    [Fact]
    public async Task Ratio_above_one_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/hedges/calculate", new
        {
            baseCurrency = "USD",
            exposures = new[] { new { currency = "EUR", exposure = 100m, hedgeRatio = 1.5m } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("HedgeRatio must be between 0 and 1", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Missing_exposure_returns_400_instead_of_defaulting_to_zero()
    {
        var response = await _client.PostAsJsonAsync("/hedges/calculate", new
        {
            baseCurrency = "USD",
            exposures = new[] { new { currency = "EUR", hedgeRatio = 0.5m } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_currencies_return_400()
    {
        var response = await _client.PostAsJsonAsync("/hedges/calculate", new
        {
            baseCurrency = "USD",
            exposures = new[]
            {
                new { currency = "EUR", exposure = 100m, hedgeRatio = 0.5m },
                new { currency = "eur", exposure = 200m, hedgeRatio = 0.5m },
            },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Duplicate currencies", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Exposure_in_base_currency_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/hedges/calculate", new
        {
            baseCurrency = "USD",
            exposures = new[] { new { currency = "USD", exposure = 100m, hedgeRatio = 0.5m } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Empty_exposure_list_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/hedges/calculate", new
        {
            baseCurrency = "USD",
            exposures = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_id_returns_404()
    {
        var response = await _client.GetAsync($"/hedges/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
