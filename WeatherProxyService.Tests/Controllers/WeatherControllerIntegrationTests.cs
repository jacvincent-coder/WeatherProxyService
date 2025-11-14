using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using WeatherProxyService.Services;
using WeatherProxyService.Tests.DTOs;
using WeatherProxyService.Tests.TestUtilities;

namespace WeatherProxyService.Tests.Controllers
{
    public class WeatherControllerIntegrationTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly Mock<IOpenWeatherService> _weatherServiceMock;

        public WeatherControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _weatherServiceMock = new Mock<IOpenWeatherService>();

            var clientFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OpenWeather:Keys:0", "test-openweather-key" },
                { "ClientApiKeys:0", "test-key" }
            });
                });

                builder.ConfigureServices(services =>
                {
                    // Remove real OpenWeatherService
                    var descriptor = services.Single(
                        d => d.ServiceType == typeof(IOpenWeatherService));
                    services.Remove(descriptor);

                    // Add mocked version
                    services.AddScoped(_ => _weatherServiceMock.Object);
                });
            });

            _client = clientFactory.CreateClient();

            // Required for the middleware
            _client.DefaultRequestHeaders.Add("X-Api-Key", "test-key");
        }

        [Fact]
        public async Task Should_Return400_When_CityMissing()
        {
            var response = await _client.GetAsync("/api/weather?country=au");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Should_Return200_When_ServiceSuccess()
        {
            _weatherServiceMock
                .Setup(s => s.GetWeatherDescriptionAsync("Sydney", "au"))
                .ReturnsAsync((true, "clear sky", null));

            var response = await _client.GetAsync("/api/weather?city=Sydney&country=au");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseObj = await response.Content.ReadFromJsonAsync<WeatherResponseDto>();

            responseObj!.description.Should().Be("clear sky");
        }
    }
}
