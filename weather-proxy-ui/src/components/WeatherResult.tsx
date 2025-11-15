import { WeatherResponse, ErrorResponse } from "../types/WeatherTypes";

interface WeatherResultProps {
  data?: WeatherResponse;
  error?: ErrorResponse | string;
}

export default function WeatherResult({ data, error }: WeatherResultProps) {
  if (error) {
    const message = typeof error === "string" ? error : error.error;
    return <div className="error">❌ {message}</div>;
  }

  if (data) {
    return (
      <div className="success">
        ✅ Weather description: <strong>{data.description}</strong>
      </div>
    );
  }

  return null;
}
