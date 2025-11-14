using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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
        private readonly string _baseUrl;

        public OpenWeatherService(
            IHttpClientFactory httpFactory,
            IOpenWeatherKeySelector keySelector,
            IConfiguration config)
        {
            _httpFactory = httpFactory;
            _keySelector = keySelector;

            // Allow overriding in config, fallback to default OpenWeather URL
            _baseUrl = config.GetValue<string>("OpenWeather:BaseUrl")
                       ?? "https://api.openweathermap.org/data/2.5/weather";
        }

        /// <summary>
        /// Retrieves the short weather "description" field from the OpenWeather API.
        /// Selects a rotating API key using IOpenWeatherKeySelector.
        /// </summary>
        public async Task<(bool success, string? description, string? error)> GetWeatherDescriptionAsync(string city, string country)
        {
            var apiKey = _keySelector.GetNextKey();
            var client = _httpFactory.CreateClient("OpenWeatherClient");

            // Build query URL
            var url = $"{_baseUrl}?q={Uri.EscapeDataString(city)},{Uri.EscapeDataString(country)}&appid={apiKey}";

            try
            {
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
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
                return (false, null, $"Exception calling OpenWeather: {ex.Message}");
            }
        }

        /// <summary>
        /// Strongly typed model representing only the fields we need from OpenWeather response.
        /// </summary>
        private class OpenWeatherResponse
        {
            public WeatherItem[]? Weather { get; set; }
        }

        private class WeatherItem
        {
            public string? Description { get; set; }
        }
    }
}
