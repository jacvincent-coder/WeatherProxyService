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

        public ApiKeyValidationMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;

            // Load ClientApiKeys as an array from appsettings.json
            _validKeys = config.GetSection("ClientApiKeys").Get<string[]>()
                         ?? [];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedKeys))
            {
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
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Invalid API key."
                });
                return;
            }

            // Store the validated key in context and pass it onto the next middleware/controller
            context.Items["ClientApiKey"] = key;

            await _next(context);
        }
    }
}
