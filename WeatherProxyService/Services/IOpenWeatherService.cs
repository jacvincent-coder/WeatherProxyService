namespace WeatherProxyService.Services
{
    public interface IOpenWeatherService
    {
        Task<(bool success, string? description, string? error)> GetWeatherDescriptionAsync(string city, string country);
    }
}
