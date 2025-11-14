
namespace WeatherProxyService.Services
{
    public class OpenWeatherService : IOpenWeatherService
    {
        public Task<(bool success, string? description, string? error)> GetWeatherDescriptionAsync(string city, string country)
        {
            throw new NotImplementedException();
        }
    }
}
