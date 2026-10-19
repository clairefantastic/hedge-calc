import { useState, type FormEvent } from "react";
import { ApiError, calculateHedges, type CalculateHedgesResponse, type ExposureInput } from "./api";

interface Row extends ExposureInput {
  id: number;
}

let nextId = 1;
const newRow = (currency = "", exposure = "", hedgeRatio = "0.75"): Row => ({
  id: nextId++,
  currency,
  exposure,
  hedgeRatio,
});

const DIRECTION_LABEL = {
  SellForeign: "Sell forward",
  BuyForeign: "Buy forward",
  None: "No hedge",
} as const;

export default function App() {
  const [baseCurrency, setBaseCurrency] = useState("USD");
  const [rows, setRows] = useState<Row[]>([
    newRow("EUR", "1000000.00", "0.75"),
    newRow("JPY", "123457", "0.5"),
    newRow("GBP", "-250000.00", "1"),
  ]);
  const [result, setResult] = useState<CalculateHedgesResponse | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  const updateRow = (id: number, field: keyof ExposureInput, value: string) =>
    setRows((current) => current.map((r) => (r.id === id ? { ...r, [field]: value } : r)));

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    setErrors([]);
    try {
      const exposures = rows.map(({ currency, exposure, hedgeRatio }) => ({ currency, exposure, hedgeRatio }));
      setResult(await calculateHedges(baseCurrency, exposures));
    } catch (err) {
      setResult(null);
      setErrors(err instanceof ApiError ? [err.message, ...err.details] : ["Could not reach the API."]);
    } finally {
      setLoading(false);
    }
  }

  return (
    <main>
      <h1>FX Hedge Calculator</h1>
      <p className="muted">Target hedge notional = exposure × hedge ratio, rounded to each currency's minor units.</p>

      <form onSubmit={handleSubmit}>
        <label className="base">
          Base currency
          <input value={baseCurrency} maxLength={3} onChange={(e) => setBaseCurrency(e.target.value.toUpperCase())} />
        </label>

        <table>
          <thead>
            <tr>
              <th>Currency</th>
              <th>Exposure</th>
              <th>Hedge ratio (0–1)</th>
              <th aria-label="Remove" />
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id}>
                <td>
                  <input value={row.currency} maxLength={3} placeholder="EUR"
                    onChange={(e) => updateRow(row.id, "currency", e.target.value.toUpperCase())} />
                </td>
                <td>
                  <input value={row.exposure} inputMode="decimal" placeholder="1000000.00"
                    onChange={(e) => updateRow(row.id, "exposure", e.target.value)} />
                </td>
                <td>
                  <input value={row.hedgeRatio} inputMode="decimal"
                    onChange={(e) => updateRow(row.id, "hedgeRatio", e.target.value)} />
                </td>
                <td>
                  <button type="button" className="link" disabled={rows.length === 1}
                    onClick={() => setRows((current) => current.filter((r) => r.id !== row.id))}>
                    Remove
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        <div className="actions">
          <button type="button" className="secondary" onClick={() => setRows((current) => [...current, newRow()])}>
            Add currency
          </button>
          <button type="submit" disabled={loading}>{loading ? "Calculating…" : "Calculate"}</button>
        </div>
      </form>

      {errors.length > 0 && (
        <div className="error" role="alert">
          {errors.map((message) => <div key={message}>{message}</div>)}
        </div>
      )}

      {result && (
        <section>
          <h2>Results</h2>
          <p className="muted">Calculation {result.calculationId} · saved {new Date(result.createdAtUtc).toLocaleString()}</p>
          <table>
            <thead>
              <tr>
                <th>Currency</th>
                <th className="num">Exposure</th>
                <th className="num">Ratio</th>
                <th className="num">Target notional</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {result.hedges.map((h) => (
                <tr key={h.currency}>
                  <td>{h.currency}</td>
                  <td className="num">{h.exposure.toLocaleString()}</td>
                  <td className="num">{(h.hedgeRatio * 100).toFixed(2)}%</td>
                  <td className="num strong">{h.targetNotional.toLocaleString()}</td>
                  <td>{DIRECTION_LABEL[h.direction]}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}
    </main>
  );
}
