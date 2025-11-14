using Microsoft.AspNetCore.Mvc;
using WeatherProxyService.Services;

namespace WeatherProxyService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : Controller
    {
        private readonly IOpenWeatherService _openWeatherService;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(IOpenWeatherService openWeatherService, ILogger<WeatherController> logger)
        {
            _openWeatherService = openWeatherService;
            _logger = logger;
        }

    }
}
