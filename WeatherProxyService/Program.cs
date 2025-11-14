using WeatherProxyService.Middleware;
using WeatherProxyService.Services;

var builder = WebApplication.CreateBuilder(args);


// services
builder.Services.AddScoped<IOpenWeatherService, OpenWeatherService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Middleware to validate API keys
app.UseMiddleware<ApiKeyValidationMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
