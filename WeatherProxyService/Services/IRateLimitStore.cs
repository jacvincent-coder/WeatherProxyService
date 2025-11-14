namespace WeatherProxyService.Services
{
    public interface IRateLimitStore
    {
        /// <summary>
        /// Attempts to consume a request from the rate limit bucket for the given client key.
        /// Returns: (allowed, remaining, resetUtc)
        /// </summary>
        (bool allowed, int remaining, DateTime resetUtc) TryConsume(string clientKey, int limitPerHour);
    }
}
