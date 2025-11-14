using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherProxyService.Controllers;
using WeatherProxyService.Services;

namespace WeatherProxyService.Tests.Controllers
{
    public class WeatherControllerTests
    {
        private readonly Mock<IOpenWeatherService> _weatherServiceMock;
        private readonly WeatherController _controller;

        public WeatherControllerTests()
        {
            _weatherServiceMock = new Mock<IOpenWeatherService>();
            var logger = Mock.Of<ILogger<WeatherController>>();

            _controller = new WeatherController(_weatherServiceMock.Object, logger);
        }

        [Fact]
        public async Task Get_ShouldReturnBadRequest_WhenCityOrCountryMissing()
        {
            var result = await _controller.Get("", "au");

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Get_ShouldReturn502_WhenServiceFails()
        {
            _weatherServiceMock
                .Setup(s => s.GetWeatherDescriptionAsync("Sydney", "au"))
                .ReturnsAsync((false, null, "upstream error"));

            var result = await _controller.Get("Sydney", "au");

            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(502);
        }

        [Fact]
        public async Task Get_ShouldReturn200_WithWeatherDescription()
        {
            _weatherServiceMock
                .Setup(s => s.GetWeatherDescriptionAsync("Sydney", "au"))
                .ReturnsAsync((true, "clear sky", null));

            var result = await _controller.Get("Sydney", "au");

            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(new { description = "clear sky" });
        }
    }
}
