using System.Text.Json.Serialization;

namespace WeatherProxyService.Models.OpenWeather
{
    public class WeatherItem
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
