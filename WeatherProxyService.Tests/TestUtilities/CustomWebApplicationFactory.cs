using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WeatherProxyService.Tests.TestUtilities
{
    public class CustomWebApplicationFactory: WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            return base.CreateHost(builder);
        }
    }
}
