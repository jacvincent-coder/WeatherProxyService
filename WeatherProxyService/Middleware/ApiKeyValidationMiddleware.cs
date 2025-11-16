using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherProxyService.Middleware
{
    public class ApiKeyValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string[] _validKeys;
        private readonly ILogger<ApiKeyValidationMiddleware> _logger;

        public ApiKeyValidationMiddleware(RequestDelegate next, 
            IConfiguration config,
            ILogger<ApiKeyValidationMiddleware> logger)
        {
            _next = next;

            // Load ClientApiKeys as an array from appsettings.json
            _validKeys = config.GetSection("ClientApiKeys").Get<string[]>()
                         ?? [];
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("Validating API key for request {Path}", context.Request.Path);

            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedKeys))
            {
                _logger.LogWarning("Missing X-Api-Key header on request: {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Missing API key. Please provide an 'X-Api-Key' header."
                });
                return;
            }

            var key = providedKeys.First();

            if (!_validKeys.Contains(key))
            {
                _logger.LogWarning("Invalid API key attempted: {Key}", key);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Invalid API key."
                });
                return;
            }

            _logger.LogInformation("API key validated successfully: {Key}", key);
            // Store the validated key in context and pass it onto the next middleware/controller
            context.Items["ClientApiKey"] = key;

            await _next(context);
        }
    }
}
