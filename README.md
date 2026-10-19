# FX Hedge Calculator API

A small ASP.NET Core (.NET 8) service that calculates target FX hedge notionals per currency for a currency-overlay style mandate, and stores every calculation (inputs and results) with EF Core.

[![CI](https://github.com/clairefantastic/hedge-calc/actions/workflows/ci.yml/badge.svg)](https://github.com/clairefantastic/hedge-calc/actions/workflows/ci.yml)

## What it does

`POST /hedges/calculate`

```json
{
  "baseCurrency": "USD",
  "exposures": [
    { "currency": "EUR", "exposure": 1000000.00, "hedgeRatio": 0.75 },
    { "currency": "JPY", "exposure": 123457,     "hedgeRatio": 0.5 },
    { "currency": "GBP", "exposure": -250000.00, "hedgeRatio": 1 }
  ]
}
```

returns `201 Created` (with a `Location` header) and:

```json
{
  "calculationId": "…",
  "createdAtUtc": "2026-10-18T21:30:00Z",
  "baseCurrency": "USD",
  "hedges": [
    { "currency": "EUR", "exposure": 1000000.00, "hedgeRatio": 0.75, "targetNotional": 750000.00, "direction": "SellForeign" },
    { "currency": "JPY", "exposure": 123457,     "hedgeRatio": 0.5,  "targetNotional": 61729,     "direction": "SellForeign" },
    { "currency": "GBP", "exposure": -250000.00, "hedgeRatio": 1,    "targetNotional": -250000.00, "direction": "BuyForeign" }
  ]
}
```

`GET /hedges/{id}` returns a stored calculation.

## Design decisions

| Decision | Why |
|---|---|
| `decimal` everywhere for money | Base-10 arithmetic; `0.1 + 0.2 == 0.3`. `double` drifts. |
| Round to ISO 4217 minor units (JPY 0, KWD 3, default 2) | A JPY notional of 61,728.50 is not a tradeable amount. |
| `MidpointRounding.AwayFromZero` | Matches how amounts are usually quoted; .NET's default is banker's rounding (`ToEven`), so the choice is explicit and tested. |
| Negative exposure allowed | A short foreign position is real; it is hedged by *buying* forward. Zero exposure → no hedge. |
| Hedge ratio validated to [0, 1] | Over-hedging is rejected at the API boundary (400) and guarded again in the domain. |
| No cross-currency total | Summing EUR and JPY notionals is meaningless without FX rates. |
| Amounts accepted as JSON strings too | Lets JavaScript clients avoid floating-point conversion. |
| Pure `HedgeCalculator` with no I/O | Unit-testable without mocks; persistence lives in `HedgeService`. |
| `TimeProvider` injected | Clock is replaceable in tests. |
| RFC 7807 ProblemDetails for errors | Consistent, machine-readable 400/404/500 responses. |

## Project layout

```
src/HedgeCalc.Api
  Domain/        HedgeCalculator, CurrencyRules (pure logic)
  Contracts/     request/response DTOs + validation
  Data/          EF Core entities, DbContext, Migrations/
  Services/      HedgeService (calculate + persist)
  Controllers/   HedgesController
tests/HedgeCalc.Tests
  HedgeCalculatorTests   unit tests (rounding, zero/negative, bounds)
  HedgesApiTests         integration tests via WebApplicationFactory + in-memory SQLite
web/                     React + TypeScript (Vite) page that calls the API
.github/workflows/ci.yml build + test on every push / PR
```

## Run it

```bash
dotnet tool install --global dotnet-ef --version "8.0.*"   # once
dotnet restore
dotnet ef migrations add InitialCreate --project src/HedgeCalc.Api --output-dir Data/Migrations   # first time only
dotnet test
dotnet run --project src/HedgeCalc.Api        # http://localhost:5080/swagger

cd web && npm install && npm run dev          # http://localhost:5173
```

## Possible next steps

- SQL Server via Docker + Testcontainers for integration tests against the production engine
- Idempotency key on `POST` so a retried request does not store a duplicate calculation
- OpenTelemetry traces/metrics, health checks
- Pin package versions with a lock file (`RestorePackagesWithLockFile`)
