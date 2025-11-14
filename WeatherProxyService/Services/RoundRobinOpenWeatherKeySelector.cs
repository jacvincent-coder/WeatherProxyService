using Microsoft.Extensions.Configuration;
using System;
using System.Threading;

namespace WeatherProxyService.Services
{
    /// <summary>
    /// Selects OpenWeather API keys in a round-robin sequence.
    /// </summary>
    public class RoundRobinOpenWeatherKeySelector : IOpenWeatherKeySelector
    {
        private readonly string[] _keys;
        private int _index = -1;

        /// <summary>
        /// Loads OpenWeather API keys from configuration.
        /// </summary>
        /// <param name="config">Application configuration.</param>
        public RoundRobinOpenWeatherKeySelector(IConfiguration config)
        {
            // Load keys directly as an array from configuration
            _keys = config.GetSection("OpenWeather:Keys").Get<string[]>()
                    ?? Array.Empty<string>();

            if (_keys.Length == 0)
                throw new InvalidOperationException(
                    "No OpenWeather keys configured. Please configure 'OpenWeather:Keys' in appsettings.json.");
        }

        /// <summary>
        /// Returns the next API key in round-robin order.
        /// </summary>
        public string GetNextKey()
        {
            // Atomically increment to avoid race conditions
            var idx = Interlocked.Increment(ref _index);
            var arrayIndex = idx % _keys.Length;

            return _keys[arrayIndex];
        }
    }
}
