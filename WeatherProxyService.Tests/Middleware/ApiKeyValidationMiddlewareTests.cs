using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using WeatherProxyService.Middleware;

namespace WeatherProxyService.Tests.Middleware
{
    public class ApiKeyValidationMiddlewareTests
    {
        [Fact]
        public async Task Should_Return401_When_ApiKeyMissing()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?>
                        {
                        {"ClientApiKeys:0", "key-1"}
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

            var response = await client.GetAsync("/api/weather");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

}
