using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using WeatherProxyService.Middleware;

namespace WeatherProxyService.Tests.Middleware
{
    public class ApiKeyValidationMiddlewareTests
    {
        [Fact]
        public async Task Should_Return401_When_ApiKeyMissing()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            { "ClientApiKeys:0", "key-1" }
                        })
                        .Build());
                })
                .Configure(app =>
                {
                    app.UseMiddleware<ApiKeyValidationMiddleware>();

                    // terminal middleware simulating a controller
                    app.Run(async ctx => await ctx.Response.WriteAsync("OK"));
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("/api/weather");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_Return401_When_ApiKeyInvalid()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            { "ClientApiKeys:0", "key-1" }
                        })
                        .Build());
                })
                .Configure(app =>
                {
                    app.UseMiddleware<ApiKeyValidationMiddleware>();

                    app.Run(async ctx => await ctx.Response.WriteAsync("OK"));
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

            // Act
            var response = await client.GetAsync("/api/weather");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_PassThrough_When_ApiKeyValid_And_SetClientApiKeyInContext()
        {
            // Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            { "ClientApiKeys:0", "key-1" }
                        })
                        .Build());
                })
                .Configure(app =>
                {
                    app.UseMiddleware<ApiKeyValidationMiddleware>();

                    // Downstream middleware simulating a controller
                    app.Run(async ctx =>
                    {
                        // Read client API key from HttpContext.Items to verify middleware behaviour
                        var keyFromContext = ctx.Items.TryGetValue("ClientApiKey", out var val)
                            ? val?.ToString()
                            : "<missing>";

                        ctx.Response.StatusCode = StatusCodes.Status200OK;
                        await ctx.Response.WriteAsync($"OK:{keyFromContext}");
                    });
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("X-Api-Key", "key-1");

            // Act
            var response = await client.GetAsync("/api/weather");
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            body.Should().Be("OK:key-1");
        }
    }
}
