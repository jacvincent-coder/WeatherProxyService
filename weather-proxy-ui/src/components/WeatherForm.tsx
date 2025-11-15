import { useState, FormEvent } from "react";
import ApiKeyField from "./ApiKeyField";

interface WeatherFormProps {
  onSubmit: (city: string, country: string, apiKey: string) => void;
  loading: boolean;
}

export default function WeatherForm({ onSubmit, loading }: WeatherFormProps) {
  const [city, setCity] = useState("");
  const [country, setCountry] = useState("");
  const [apiKey, setApiKey] = useState("client-key-1");

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    onSubmit(city.trim(), country.trim(), apiKey.trim());
  };

  return (
    <form className="weather-form" onSubmit={handleSubmit}>
      <div className="form-row">
        <label htmlFor="city">City</label>
        <input
          id="city"
          value={city}
          onChange={(e) => setCity(e.target.value)}
          placeholder="e.g. Sydney"
        />
      </div>

      <div className="form-row">
        <label htmlFor="country">Country</label>
        <input
          id="country"
          value={country}
          onChange={(e) => setCountry(e.target.value)}
          placeholder="e.g. au"
        />
      </div>

      <ApiKeyField value={apiKey} onChange={setApiKey} />

      <button type="submit" disabled={loading}>
        {loading ? "Loading..." : "Get Weather"}
      </button>
    </form>
  );
}
