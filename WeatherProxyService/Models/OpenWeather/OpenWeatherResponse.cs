using System.Text.Json.Serialization;

namespace WeatherProxyService.Models.OpenWeather
{
    public class OpenWeatherResponse
    {
        [JsonPropertyName("weather")]
        public WeatherItem[]? Weather { get; set; }
    }
}
