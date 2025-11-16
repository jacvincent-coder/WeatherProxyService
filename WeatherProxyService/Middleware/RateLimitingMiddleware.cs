using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using WeatherProxyService.Services;

namespace WeatherProxyService.Middleware
{
    /// <summary>
    /// Middleware responsible for enforcing per-client API rate limiting.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimitStore _store;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        // API limit configuration
        private const int LIMIT_PER_HOUR = 5;

        /// <summary>
        /// Constructs the rate limiting middleware.
        /// </summary>
        /// <param name="next">The next middleware in the ASP.NET Core pipeline.</param>
        /// <param name="store">The rate limit storage provider (in-memory implementation).</param>
        public RateLimitingMiddleware(RequestDelegate next, 
            IRateLimitStore store,
            ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _store = store;
            _logger = logger;
        }

        /// <summary>
        /// Middleware execution method. Applies rate limiting to the /api/weather endpoint.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {

            // Only apply rate limiting to the weather endpoint
            if (context.Request.Path.StartsWithSegments("/api/weather", StringComparison.OrdinalIgnoreCase))
            {
                // Retrieve the validated client API key from HttpContext
                if (!context.Items.TryGetValue("ClientApiKey", out var clientKeyObj)
                    || clientKeyObj is not string clientApiKey)
                {
                    _logger.LogInformation("API Key missing or invalid before rate limiting.");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "API key missing or invalid before rate limiting."
                    });
                    return;
                }

                // Evaluate and update rate limit usage
                var (allowed, remaining, resetUtc) = _store.TryConsume(clientApiKey, LIMIT_PER_HOUR);

                if (!allowed)
                {
                    _logger.LogWarning("Rate limit exceeded for {Key}. Remaining = {Remaining}", clientApiKey, remaining);
                    // Too many requests
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    // Retry-After header (in seconds)
                    var retrySeconds = Math.Max(0, (int)(resetUtc - DateTime.UtcNow).TotalSeconds);
                    context.Response.Headers["Retry-After"] = retrySeconds.ToString();

                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Hourly rate limit exceeded",
                        message = $"This API key is limited to {LIMIT_PER_HOUR} calls per hour.",
                        limit = LIMIT_PER_HOUR,
                        remaining,
                        resetUtc = resetUtc.ToString("o")
                    });

                    return;
                }

                _logger.LogInformation("Rate limit OK for {Key}. Remaining = {Remaining}", clientApiKey, remaining);

                // Add rate-limit headers for maximum client visibility
                context.Response.Headers["X-RateLimit-Limit"] = LIMIT_PER_HOUR.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = resetUtc.ToString("o");
            }

            // Continue to next middleware or endpoint
            await _next(context);
        }
    }
}
