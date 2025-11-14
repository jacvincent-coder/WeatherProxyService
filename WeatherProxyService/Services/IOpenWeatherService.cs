namespace WeatherProxyService.Services
{
    /// <summary>
    /// Provides a wrapper around the OpenWeather API for retrieving weather information.
    /// </summary>
    public interface IOpenWeatherService
    {
        /// <summary>
        /// Retrieves the "description" field of the weather data for the specified city and country.
        /// </summary>
        /// <param name="city">Name of the city.</param>
        /// <param name="country">Name or code of the country.</param>
        /// <returns>
        /// A tuple containing:
        ///   success - whether the call succeeded,
        ///   description - the weather description,
        ///   error - an error message.
        /// </returns>
        Task<(bool success, string? description, string? error)> GetWeatherDescriptionAsync(string city, string country);
    }
}
