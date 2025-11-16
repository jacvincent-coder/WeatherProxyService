using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WeatherProxyService.Services;

namespace WeatherProxyService.Tests.Services
{
    public class OpenWeatherServiceTests
    {
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

        private ILogger<OpenWeatherService> CreateLogger()
        {
            // Create a dummy logger that does nothing
            return Mock.Of<ILogger<OpenWeatherService>>();
        }

        [Fact]
        public async Task Should_ReturnDescription_OnSuccess()
        {
            // Arrange
            var json = @"{ ""weather"": [{ ""description"": ""rainy"" }] }";

            var httpClient = CreateHttpClient(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("OpenWeatherClient"))
                             .Returns(httpClient);

            var selector = new Mock<IOpenWeatherKeySelector>();
            selector.Setup(s => s.GetNextKey()).Returns("dummy-key");

            var settings = new Dictionary<string, string>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

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
            success.Should().BeTrue();
            desc.Should().Be("rainy");
            error.Should().BeNull();
        }

        [Fact]
        public async Task Should_ReturnError_WhenUpstreamFails()
        {
            // Arrange
            var httpClient = CreateHttpClient(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Invalid request")
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
            var (success, desc, error) =
                await service.GetWeatherDescriptionAsync("Sydney", "au");

            // Assert
            success.Should().BeFalse();
            desc.Should().BeNull();
            error!.Should().Contain("Upstream error");
        }
    }
}
