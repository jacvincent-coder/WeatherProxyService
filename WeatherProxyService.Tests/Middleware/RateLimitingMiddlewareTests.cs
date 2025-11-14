using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using WeatherProxyService.Middleware;
using WeatherProxyService.Services;

namespace WeatherProxyService.Tests.Middleware
{
    public class RateLimitingMiddlewareTests
    {
        [Fact]
        public async Task Should_Return429_When_RateLimitExceeded()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddMemoryCache();
                    services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
                })
                .Configure(app =>
                {
                    // mimic validated API key already present
                    app.Use(async (ctx, next) =>
                    {
                        ctx.Items["ClientApiKey"] = "test-key";
                        await next();
                    });

                    app.UseMiddleware<RateLimitingMiddleware>();

                    app.Run(async ctx => await ctx.Response.WriteAsync("OK"));
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            // hit limit (default is 5)
            for (int i = 0; i < 5; i++)
                (await client.GetAsync("/api/weather")).StatusCode.Should().Be(HttpStatusCode.OK);

            var final = await client.GetAsync("/api/weather");

            final.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }
    }

}
