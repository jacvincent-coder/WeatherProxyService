using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherProxyService.Services;

namespace WeatherProxyService.Tests.Services
{
    public class InMemoryRateLimitStoreTests
    {
        [Fact]
        public void Should_AllowFiveRequests_ThenBlock()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger<InMemoryRateLimitStore>>().Object;
            var store = new InMemoryRateLimitStore(cache, logger);


            for (int i = 0; i < 5; i++)
            {
                var (allowed, remaining, _) = store.TryConsume("test-key", 5);
                allowed.Should().BeTrue();
            }

            var (allowedFinal, _, _) = store.TryConsume("test-key", 5);

            allowedFinal.Should().BeFalse();
        }
    }

}
