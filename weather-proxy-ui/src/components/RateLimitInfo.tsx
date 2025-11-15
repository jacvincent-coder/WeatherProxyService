import { RateLimitHeaders } from "../types/WeatherTypes";

interface Props {
  rateLimit?: RateLimitHeaders;
}

export default function RateLimitInfo({ rateLimit }: Props) {
  if (!rateLimit) return null;

  return (
    <div className="rate-limit-box">
      <p><strong>Rate Limit Info</strong></p>
      <p>Limit: {rateLimit.limit ?? "-"}</p>
      <p>Remaining: {rateLimit.remaining ?? "-"}</p>
      <p>Reset: {rateLimit.reset ?? "-"}</p>
    </div>
  );
}
