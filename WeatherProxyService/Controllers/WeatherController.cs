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
        /// 400 BadRequest Geocoding mismatch  
        /// 502 BadGateway if OpenWeather API fails  
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string city, [FromQuery] string country)
        {
            _logger.LogInformation("Weather endpoint hit for City={City}, Country={Country}", city, country);

            if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country))
            {
                _logger.LogWarning("BadRequest – Missing parameters. City='{City}', Country='{Country}'", city, country);

                return BadRequest(new
                {
                    error = "Both 'city' and 'country' parameters are required."
                });
            }

            city = city.Trim();
            country = country.Trim();

            var (success, description, error) =
                await _openWeather.GetWeatherDescriptionAsync(city, country);

            // Geocoding OR weather failure both come through here,
            // but we distinguish based on message content.
            if (!success)
            {
                if (error != null && error.StartsWith("No matching city", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Validation failed for {City}, {Country}: {Error}", city, country, error);

                    return BadRequest(new
                    {
                        error = "City and country validation failed.",
                        details = error
                    });
                }

                // Weather lookup failure → treat as 502 BadGateway
                _logger.LogWarning("Upstream weather failure for {City}/{Country}: {Error}", city, country, error);

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
