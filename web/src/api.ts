export type HedgeDirection = "None" | "SellForeign" | "BuyForeign";

export interface ExposureInput {
  currency: string;
  // Sent as strings so amounts are never converted to JavaScript floating-point numbers.
  exposure: string;
  hedgeRatio: string;
}

export interface HedgeLine {
  currency: string;
  exposure: number;
  hedgeRatio: number;
  targetNotional: number;
  direction: HedgeDirection;
}

export interface CalculateHedgesResponse {
  calculationId: string;
  createdAtUtc: string;
  baseCurrency: string;
  hedges: HedgeLine[];
}

/** Shape of ASP.NET Core's ValidationProblemDetails (RFC 7807). */
interface ProblemDetails {
  title?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(message: string, public readonly details: string[]) {
    super(message);
  }
}

export async function calculateHedges(
  baseCurrency: string,
  exposures: ExposureInput[],
): Promise<CalculateHedgesResponse> {
  const response = await fetch("/hedges/calculate", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ baseCurrency, exposures }),
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails;
    const details = Object.values(problem.errors ?? {}).flat();
    throw new ApiError(problem.title ?? `Request failed (${response.status})`, details);
  }

  return (await response.json()) as CalculateHedgesResponse;
}
