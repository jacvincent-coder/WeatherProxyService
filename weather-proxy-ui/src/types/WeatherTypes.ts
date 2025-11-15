export interface WeatherResponse {
  description: string;
}

export interface ErrorResponse {
  error: string;
  details?: string;
}

export interface RateLimitHeaders {
  limit: string | null;
  remaining: string | null;
  reset: string | null;
}
