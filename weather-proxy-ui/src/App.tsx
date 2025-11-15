import { useState } from "react";
import "./App.css";
import WeatherForm from "./components/WeatherForm";
import WeatherResult from "./components/WeatherResult";
import RateLimitInfo from "./components/RateLimitInfo";
import { fetchWeather } from "./api/weatherClient";
import { WeatherResponse, ErrorResponse, RateLimitHeaders } from "./types/WeatherTypes";

function App() {
  const [weather, setWeather] = useState<WeatherResponse | undefined>();
  const [error, setError] = useState<ErrorResponse | string | undefined>();
  const [rateLimit, setRateLimit] = useState<RateLimitHeaders>();
  const [loading, setLoading] = useState(false);

  const handleWeatherRequest = async (city: string, country: string, apiKey: string) => {
    setLoading(true);
    setError(undefined);
    setWeather(undefined);

    const result = await fetchWeather(city, country, apiKey);

    setRateLimit(result.rateLimit);

    if (result.success) {
      setWeather(result.data);
    } else {
      setError(result.error?.error ?? "Unexpected error");
    }

    setLoading(false);
  };

  return (
    <div className="app">
      <h1>Weather Proxy Client</h1>
      <WeatherForm onSubmit={handleWeatherRequest} loading={loading} />
      
      {loading && <div>Loading...</div>}

      <WeatherResult data={weather} error={error} />
      <RateLimitInfo rateLimit={rateLimit} />
    </div>
  );
}

export default App;
