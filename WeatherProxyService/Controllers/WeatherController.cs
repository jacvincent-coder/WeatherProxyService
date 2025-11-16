using Microsoft.AspNetCore.Mvc;
using WeatherProxyService.Services;

namespace WeatherProxyService.Controllers
{
    /// <summary>
    /// Exposes an endpoint for retrieving weather information through the
    /// WeatherProxyService. This controller delegates weather retrieval to the
    /// IOpenWeatherService, which handles outbound calls to the OpenWeather API.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly IOpenWeatherService _openWeather;
        private readonly ILogger<WeatherController> _logger;

        /// <summary>
        /// Constructs the controller with its required dependencies.
        /// </summary>
        public WeatherController(
            IOpenWeatherService openWeather,
            ILogger<WeatherController> logger)
        {
            _openWeather = openWeather;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a descriptive weather summary for the specified city and country.
        /// </summary>
        /// <param name="city">City name.</param>
        /// <param name="country">Country name or country code.</param>
        /// <returns>
        /// 200 OK with { description = "..."} on success  
        /// 400 BadRequest if parameters are missing  
        /// 502 BadGateway if OpenWeather API fails  
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string city, [FromQuery] string country)
        {
            _logger.LogInformation("Weather endpoint hit for {City}, {Country}", city, country);

            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country))
            {
                _logger.LogWarning(
                    "Bad request: missing city or country parameter. City='{City}', Country='{Country}'",
                    city, country);
                return BadRequest(new
                {
                    error = "Both 'city' and 'country' query parameters are required."
                });
            }

            city = city.Trim();
            country = country.Trim();

            // Call service layer
            var (success, description, error) = await _openWeather.GetWeatherDescriptionAsync(city, country);

            if (!success)
            {
                _logger.LogWarning(
                    "Failed to retrieve weather for {City}, {Country}. Error: {Error}",
                    city, country, error);

                return StatusCode(502, new
                {
                    error = "Failed to retrieve weather from upstream provider.",
                    details = error
                });
            }

            return Ok(new { description });
        }
    }
}
