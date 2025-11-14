namespace WeatherProxyService.Services
{
    /// <summary>
    /// Provides a strategy for selecting which OpenWeather API key
    /// should be used for the next outbound request.
    /// </summary>
    public interface IOpenWeatherKeySelector
    {
        /// <summary>
        /// Returns the API key that should be used for the next request to OpenWeather.
        /// </summary>
        /// <returns>A valid OpenWeather API key as a string.</returns>
        string GetNextKey();
    }
}
