using WeatherProxyService.Middleware;
using WeatherProxyService.Services;

var builder = WebApplication.CreateBuilder(args);


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
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
var openWeatherKeys = app.Configuration.GetSection("OpenWeather:Keys").Get<string[]>();
if (openWeatherKeys == null || openWeatherKeys.Length == 0)
    throw new Exception("OpenWeather keys are not configured. Please set them in appsettings.json or environment variables.");

var clientKeys = app.Configuration.GetSection("ClientApiKeys").Get<string[]>();
if (clientKeys == null || clientKeys.Length == 0)
    throw new Exception("Client API keys are not configured. Please set them in appsettings.json or environment variables.");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware to validate API keys and enforce rate limiting
app.UseMiddleware<ApiKeyValidationMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the Program class public for integration testing
public partial class Program { }
