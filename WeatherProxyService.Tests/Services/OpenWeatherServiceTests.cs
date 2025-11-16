using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WeatherProxyService.Models.GeoCoding;
using WeatherProxyService.Models.OpenWeather;
using WeatherProxyService.Services;

namespace WeatherProxyService.Tests.Services
{
    public class OpenWeatherServiceTests
    {
        private ILogger<OpenWeatherService> CreateLogger() =>
            Mock.Of<ILogger<OpenWeatherService>>();

        private HttpClient CreateHttpClient(HttpResponseMessage response)
        {
            var handler = new Mock<HttpMessageHandler>();

            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);

            return new HttpClient(handler.Object);
        }

        private HttpClient CreateSequenceClient(params HttpResponseMessage[] responses)
        {
            var handler = new Mock<HttpMessageHandler>();
            var queue = new Queue<HttpResponseMessage>(responses);

            handler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() => queue.Dequeue());

            return new HttpClient(handler.Object);
        }

        [Fact]
        public async Task ValidateCityCountry_ShouldReturnCoordinates_WhenValid()
        {
            // Arrange
            var geocodeJson = @"[
                { ""lat"": -33.8688, ""lon"": 151.2093 }
            ]";

            var httpClient = CreateHttpClient(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geocodeJson)
            });

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("OpenWeatherClient"))
                             .Returns(httpClient);

            var selector = new Mock<IOpenWeatherKeySelector>();
            selector.Setup(s => s.GetNextKey()).Returns("dummy-key");

            var config = new ConfigurationBuilder().Build();
            var logger = CreateLogger();

            var service = new OpenWeatherService(
                httpClientFactory.Object,
                selector.Object,
                config,
                logger
            );

            // Act
            var (success, lat, lon, error) =
                await service.ValidateCityCountryAsync("Sydney", "au");

            // Assert
            success.Should().BeTrue();
            lat.Should().Be(-33.8688);
            lon.Should().Be(151.2093);
            error.Should().BeNull();
        }

        [Fact]
        public async Task ValidateCityCountry_ShouldReturnError_WhenNoResults()
        {
            // Arrange
            var geocodeJson = "[]";

            var httpClient = CreateHttpClient(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geocodeJson)
            });

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("OpenWeatherClient"))
                             .Returns(httpClient);

            var selector = new Mock<IOpenWeatherKeySelector>();
            selector.Setup(s => s.GetNextKey()).Returns("dummy-key");

            var config = new ConfigurationBuilder().Build();
            var logger = CreateLogger();

            var service = new OpenWeatherService(
                httpClientFactory.Object,
                selector.Object,
                config,
                logger
            );

            // Act
            var (success, _, _, error) =
                await service.ValidateCityCountryAsync("FakeCity", "xx");

            // Assert
            success.Should().BeFalse();
            error.Should().Contain("No matching city");
        }

        [Fact]
        public async Task Should_ReturnDescription_AfterSuccessfulValidation()
        {
            // Arrange
            var geocodeJson = @"[{ ""lat"": -33.8688, ""lon"": 151.2093 }]";
            var weatherJson = @"{ ""weather"": [{ ""description"": ""sunny"" }] }";

            // Two responses in sequence: Geocode → Weather
            var httpClient = CreateSequenceClient(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(geocodeJson)
                },
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(weatherJson)
                }
            );

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("OpenWeatherClient"))
                             .Returns(httpClient);

            var selector = new Mock<IOpenWeatherKeySelector>();
            selector.SetupSequence(s => s.GetNextKey())
                    .Returns("key1") // for geocoding
                    .Returns("key2"); // for weather

            var config = new ConfigurationBuilder().Build();
            var logger = CreateLogger();

            var service = new OpenWeatherService(
                httpClientFactory.Object,
                selector.Object,
                config,
                logger
            );

            // Act
            var (success, description, error) =
                await service.GetWeatherDescriptionAsync("Sydney", "au");

            // Assert
            success.Should().BeTrue();
            description.Should().Be("sunny");
            error.Should().BeNull();
        }

        [Fact]
        public async Task Should_ReturnError_WhenWeatherApiFails()
        {
            // Arrange
            var geocodeJson = @"[{ ""lat"": -33.8688, ""lon"": 151.2093 }]";

            // Geocode OK → Weather fails
            var httpClient = CreateSequenceClient(
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(geocodeJson)
                },
                new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Invalid weather request")
                }
            );

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("OpenWeatherClient"))
                             .Returns(httpClient);

            var selector = new Mock<IOpenWeatherKeySelector>();
            selector.SetupSequence(s => s.GetNextKey())
                    .Returns("key1")
                    .Returns("key2");

            var config = new ConfigurationBuilder().Build();
            var logger = CreateLogger();

            var service = new OpenWeatherService(
                httpClientFactory.Object,
                selector.Object,
                config,
                logger
            );

            // Act
            var (success, desc, error) =
                await service.GetWeatherDescriptionAsync("Sydney", "au");

            // Assert
            success.Should().BeFalse();
            desc.Should().BeNull();
            error.Should().Contain("Upstream error");
        }
    }
}
