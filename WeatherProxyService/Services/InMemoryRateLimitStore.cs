using Microsoft.Extensions.Caching.Memory;
using System;

namespace WeatherProxyService.Services
{
    /// <summary>
    /// A simple fixed-window rate limiter stored in-memory.
    /// </summary>
    public class InMemoryRateLimitStore : IRateLimitStore
    {
        private readonly IMemoryCache _cache;
        private readonly object _lock = new();

        public InMemoryRateLimitStore(IMemoryCache cache)
        {
            _cache = cache;
        }

        public (bool allowed, int remaining, DateTime resetUtc) TryConsume(string clientKey, int limitPerHour)
        {
            var cacheKey = $"ratelimit:{clientKey}";
            lock (_lock)
            {
                var now = DateTime.UtcNow;

                // Retrieve the user's current window (if any)
                if (!_cache.TryGetValue(cacheKey, out RateWindow window))
                {
                    window = CreateNewWindow(now);
                }

                var windowEnd = window.WindowStart.AddHours(1);

                // If the window expired - reset for the new hour
                if (now >= windowEnd)
                {
                    window = CreateNewWindow(now);
                }

                // If limit exceeded - reject request
                if (window.Count >= limitPerHour)
                {
                    return (false, remaining: 0, resetUtc: windowEnd);
                }

                // Consume the request
                window.Count++;
                var remainingAfter = Math.Max(0, limitPerHour - window.Count);

                // Save updated window with expiration aligned to the window end time
                _cache.Set(
                    cacheKey,
                    window,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = windowEnd
                    });

                return (true, remainingAfter, windowEnd);
            }
        }

        /// <summary>
        /// Creates a new rate window aligned to the current hour (e.g., 12:00:00 UTC - 12:59:59 UTC).
        /// </summary>
        private static RateWindow CreateNewWindow(DateTime now)
        {
            var windowStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

            return new RateWindow
            {
                WindowStart = windowStart,
                Count = 0
            };
        }

        /// <summary>
        /// Represents a single client's hourly rate limit window.
        /// </summary>
        private class RateWindow
        {
            public DateTime WindowStart { get; set; }
            public int Count { get; set; }
        }
    }
}
