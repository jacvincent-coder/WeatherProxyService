using WeatherProxyService.Middleware;
using WeatherProxyService.Services;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Structured Logging
//builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddApplicationInsights(
    configureTelemetryConfiguration: (config) => { },
    configureApplicationInsightsLoggerOptions: (options) =>
    {
        options.TrackExceptionsAsExceptionTelemetry = true;
    });


// Application Insights logging and Telemetry
builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
{
    ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"],
    EnableAdaptiveSampling = false
});


// services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IOpenWeatherKeySelector, RoundRobinOpenWeatherKeySelector>();
builder.Services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
builder.Services.AddScoped<IOpenWeatherService, OpenWeatherService>();
builder.Services.AddHttpClient("OpenWeatherClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});


builder.Services.AddControllers();

// Swagger with API key support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Client API Key required",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Name = "X-Api-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Validate config on startup

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>()
                               .CreateLogger("Startup");

var openWeatherKeys = app.Configuration.GetSection("OpenWeather:Keys").Get<string[]>();
if (openWeatherKeys == null || openWeatherKeys.Length == 0)
{
    startupLogger.LogCritical("OpenWeather API keys missing. Service cannot start.");
    throw new InvalidOperationException("OpenWeather API keys are not configured. Please set them in appsettings.json or environment variables.");
}
    

var clientKeys = app.Configuration.GetSection("ClientApiKeys").Get<string[]>();
if (clientKeys == null || clientKeys.Length == 0)
{
    startupLogger.LogCritical("Client API keys missing. Service cannot start.");
    throw new InvalidOperationException("Client API keys are not configured. Please set them in appsettings.json or environment variables.");
}

startupLogger.LogInformation("WeatherProxyService startup validation succeeded. Loaded {Count} OpenWeather keys and {ClientCount} client API keys.",
    openWeatherKeys.Length, clientKeys.Length);


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handling middleware
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                                           .CreateLogger("GlobalExceptionHandler");

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        logger.LogError(exception, "Unhandled exception occurred while processing request");

        await context.Response.WriteAsJsonAsync(new
        {
            error = "Internal server error",
            details = exception?.Message
        });
    });
});

// Middleware to validate API keys and enforce rate limiting
app.UseMiddleware<ApiKeyValidationMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


startupLogger.LogInformation("WeatherProxyService is now running.");

app.Run();

// Make the Program class public for integration testing
public partial class Program { }
