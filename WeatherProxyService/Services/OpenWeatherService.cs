using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WeatherProxyService.Helpers;
using WeatherProxyService.Models.GeoCoding;
using WeatherProxyService.Models.OpenWeather;

namespace WeatherProxyService.Services
{
    /// <summary>
    /// Handles outbound requests to the OpenWeather API, selecting API keys
    /// via the IOpenWeatherKeySelector strategy.
    /// </summary>
    public class OpenWeatherService : IOpenWeatherService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IOpenWeatherKeySelector _keySelector;
        private readonly ILogger<OpenWeatherService> _logger;

        private readonly string _weatherBaseUrl;
        private readonly string _geoBaseUrl;

        public OpenWeatherService(
            IHttpClientFactory httpFactory,
            IOpenWeatherKeySelector keySelector,
            IConfiguration config,
            ILogger<OpenWeatherService> logger)
        {
            _httpFactory = httpFactory;
            _keySelector = keySelector;

            // Allow overriding in config, fallback to default OpenWeather URL
            _weatherBaseUrl = config.GetValue<string>("OpenWeather:BaseUrl")
                             ?? "https://api.openweathermap.org/data/2.5/weather";

            _geoBaseUrl = config.GetValue<string>("OpenWeather:GeocodeUrl")
                          ?? "http://api.openweathermap.org/geo/1.0/direct";

            _logger = logger;
        }

        /// <summary>
        /// Retrieves the short weather "description" field from the OpenWeather API.
        /// Selects a rotating API key using IOpenWeatherKeySelector.
        /// </summary>
        public async Task<(bool success, string? description, string? error)>GetWeatherDescriptionAsync(string city, string country)
        {
            _logger.LogInformation("Calling OpenWeather for City={City}, Country={Country}",city, country);

            var apiKey = _keySelector.GetNextKey();
            var client = _httpFactory.CreateClient("OpenWeatherClient");

            // validate using Geocoding API for better correctness
            var (valid, lat, lon, validationError) = await ValidateCityCountryAsync(city, country);

            if (!valid)
            {
                _logger.LogWarning("Validation failed for City={City}, Country={Country}. Error={Error}",
                    city, country, validationError);

                return (false, null, validationError);
            }

            var url = $"{_weatherBaseUrl}?lat={lat}&lon={lon}&appid={apiKey}";

            try
            {
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "OpenWeather returned {StatusCode} for {City}/{Country}",
                        response.StatusCode, city, country);

                    var body = await response.Content.ReadAsStringAsync();
                    return (false, null, $"Upstream error {response.StatusCode}: {body}");
                }

                var model = await response.Content.ReadFromJsonAsync<OpenWeatherResponse>();

                if (model?.Weather != null && model.Weather.Length > 0)
                {
                    return (true, model.Weather[0].Description, null);
                }

                return (false, null, "Weather description not found in OpenWeather response.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception calling OpenWeather for City={City}/{Country}",
                    city, country);

                return (false, null, $"Exception calling OpenWeather: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that the given city + country represent a valid location
        /// using OpenWeather’s Geocoding API. Returns lat/lon coordinates if valid.
        /// </summary>
        public async Task<(bool success, double lat, double lon, string? error)> ValidateCityCountryAsync(string city, string country)
        {
            _logger.LogInformation(
                "Validating City={City}, Country={Country} via Geocoding API",
                city, country);

            var apiKey = _keySelector.GetNextKey();
            var client = _httpFactory.CreateClient("OpenWeatherClient");

            // Normalize country
            var normalizedCountry = CountryCodeMapper.NormalizeToIso(country);

            // If we cannot map the country input then reject immediately
            if (normalizedCountry == null)
            {
                return (false, 0, 0, $"Unknown country '{country}'. Please use a valid country name or ISO code.");
            }

            var url =
                $"{_geoBaseUrl}?q={Uri.EscapeDataString(city)},{Uri.EscapeDataString(normalizedCountry)}&limit=1&appid={apiKey}";

            try
            {
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Geocoding returned non-success {StatusCode} for {City}/{Country}",
                        response.StatusCode, city, country);

                    var body = await response.Content.ReadAsStringAsync();
                    return (false, 0, 0, $"Geocoding error {response.StatusCode}: {body}");
                }

                var results = await response.Content.ReadFromJsonAsync<GeoCodingResponse[]>();

                if (results == null || results.Length == 0)
                {
                    _logger.LogWarning(
                        "City/Country mismatch detected. No geocoding results for {City}/{Country}",
                        city, country);

                    return (false, 0, 0,
                        "No matching city found for the specified country. Please check spelling or country code.");
                }

                var match = results[0];

                // Validate strict country match
                if (!string.Equals(match.Country, normalizedCountry, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "City/Country mismatch detected. No geocoding results for {City}/{Country}",
                        city, country);

                    return (false, 0, 0,
                        $"City '{city}' does not belong to country '{country}'. Found: '{match.Country}'");
                }

                _logger.LogInformation(
                    "Validation succeeded for {City}/{Country}. Lat={Lat}, Lon={Lon}",
                    city, country, match.Latitude, match.Longitude);

                return (true, match.Latitude, match.Longitude, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception occurred during geocoding validation for {City}/{Country}",city, country);

                return (false, 0, 0, $"Exception calling Geocoding API: {ex.Message}");
            }
        }
    }
}
