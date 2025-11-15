import { WeatherResponse, ErrorResponse, RateLimitHeaders } from "../types/WeatherTypes";

export async function fetchWeather(
  city: string,
  country: string,
  apiKey: string
): Promise<{
  success: boolean;
  data?: WeatherResponse;
  error?: ErrorResponse;
  rateLimit?: RateLimitHeaders;
}> {
  const query = new URLSearchParams({ city, country }).toString();

  const response = await fetch(`/api/weather?${query}`, {
    headers: {
      "X-Api-Key": apiKey,
    },
  });

  const rateLimit: RateLimitHeaders = {
    limit: response.headers.get("X-RateLimit-Limit") ?? null,
    remaining: response.headers.get("X-RateLimit-Remaining") ?? null,
    reset: response.headers.get("X-RateLimit-Reset") ?? null,
  };

  const raw = await response.json().catch(() => ({}));

  if (!response.ok) {
    return {
      success: false,
      error: raw as ErrorResponse,
      rateLimit,
    };
  }

  return {
    success: true,
    data: raw as WeatherResponse,
    rateLimit,
  };
}
