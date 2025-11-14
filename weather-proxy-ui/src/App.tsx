import { useState, FormEvent } from "react";
import "./App.css";
import { WeatherResponse, ErrorResponse } from "./types/WeatherTypes";

function App() {
  const [city, setCity] = useState<string>("");
  const [country, setCountry] = useState<string>("");
  const [apiKey, setApiKey] = useState<string>("client-key-1");
  const [description, setDescription] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setDescription(null);

    if (!city.trim() || !country.trim()) {
      setError("Both city and country are required.");
      return;
    }

    if (!apiKey.trim()) {
      setError("API key (X-Api-Key) is required.");
      return;
    }

    setIsLoading(true);

    try {
      const query = new URLSearchParams({
        city: city.trim(),
        country: country.trim(),
      }).toString();

      const response = await fetch(`/api/weather?${query}`, {
        headers: {
          "X-Api-Key": apiKey.trim(),
        },
      });

      const data = (await response.json()) as WeatherResponse | ErrorResponse;

      if (!response.ok) {
        const err = data as ErrorResponse;
        setError(err.error || "An error occurred.");
        return;
      }

      const successData = data as WeatherResponse;
      setDescription(successData.description);
    } catch (err: any) {
      setError(`Unexpected error: ${err.message}`);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="app">
      <h1>Weather Proxy Client</h1>
      <p>Enter a city and country to fetch the weather description.</p>

      <form className="weather-form" onSubmit={handleSubmit}>
        <div className="form-row">
          <label htmlFor="city">City</label>
          <input
            id="city"
            type="text"
            value={city}
            onChange={(e) => setCity(e.target.value)}
            placeholder="e.g. Sydney"
          />
        </div>

        <div className="form-row">
          <label htmlFor="country">Country</label>
          <input
            id="country"
            type="text"
            value={country}
            onChange={(e) => setCountry(e.target.value)}
            placeholder="e.g. au or Australia"
          />
        </div>

        <div className="form-row">
          <label htmlFor="apiKey">X-Api-Key</label>
          <input
            id="apiKey"
            type="text"
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            placeholder="client-key-1"
          />
        </div>

        <button type="submit" disabled={isLoading}>
          {isLoading ? "Loading..." : "Get Weather"}
        </button>
      </form>

      <div className="results">
        {error && <div className="error">❌ {error}</div>}
        {description && !error && (
          <div className="success">
            ✅ Weather description: <strong>{description}</strong>
          </div>
        )}
      </div>
    </div>
  );
}

export default App;
