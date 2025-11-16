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
        /// <summary>
        /// Helper test server factory for reusability
        /// </summary>
        private TestServer CreateServer(IRateLimitStore? storeOverride = null)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddMemoryCache();

                    if (storeOverride != null)
                        services.AddSingleton(storeOverride);
                    else
                        services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
                })
                .Configure(app =>
                {
                    // Pre-populate context with validated API key
                    app.Use(async (ctx, next) =>
                    {
                        ctx.Items["ClientApiKey"] = "test-key";
                        await next();
                    });

                    app.UseMiddleware<RateLimitingMiddleware>();

                    // Terminal middleware simulating a controller
                    app.Run(async ctx => await ctx.Response.WriteAsync("OK"));
                });

            return new TestServer(builder);
        }

        [Fact]
        public async Task Should_Return429_When_RateLimitExceeded()
        {
            var server = CreateServer();
            var client = server.CreateClient();

            // Hit limit (5 allowed)
            for (int i = 0; i < 5; i++)
            {
                var res = await client.GetAsync("/api/weather");
                res.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // 6th request should be rejected
            var final = await client.GetAsync("/api/weather");
            final.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

            final.Headers.Contains("Retry-After").Should().BeTrue();
        }

        [Fact]
        public async Task Should_NotApplyRateLimiting_ForOtherEndpoints()
        {
            var server = CreateServer();
            var client = server.CreateClient();

            var response = await client.GetAsync("/api/health");

            // Should not be rate-limited
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        [Fact]
        public async Task Should_Return401_When_ClientKeyMissing()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddMemoryCache();
                    services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
                })
                .Configure(app =>
                {
                    // DO NOT set ctx.Items["ClientApiKey"]
                    app.UseMiddleware<RateLimitingMiddleware>();

                    app.Run(async ctx => await ctx.Response.WriteAsync("OK"));
                });

            var server = new TestServer(builder);
            var client = server.CreateClient();

            var response = await client.GetAsync("/api/weather");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Should_IncludeRateLimitHeaders_OnSuccess()
        {
            var server = CreateServer();
            var client = server.CreateClient();

            var response = await client.GetAsync("/api/weather");

            response.Headers.Contains("X-RateLimit-Limit").Should().BeTrue();
            response.Headers.Contains("X-RateLimit-Remaining").Should().BeTrue();
            response.Headers.Contains("X-RateLimit-Reset").Should().BeTrue();
        }

        [Fact]
        public async Task Should_Respect_CustomRateLimitStore()
        {
            // Custom store that always blocks
            var fakeStore = new FakeRateLimitStore();

            var server = CreateServer(fakeStore);
            var client = server.CreateClient();

            var response = await client.GetAsync("/api/weather");

            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        private class FakeRateLimitStore : IRateLimitStore
        {
            public (bool allowed, int remaining, DateTime resetUtc)
                TryConsume(string clientKey, int limitPerHour)
            {
                return (false, 0, DateTime.UtcNow.AddMinutes(10));
            }
        }
    }
}
