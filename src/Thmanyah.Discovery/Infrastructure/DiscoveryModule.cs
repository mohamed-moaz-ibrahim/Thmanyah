using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Thmanyah.Discovery.Infrastructure;
using Thmanyah.Discovery.Services;
using Thmanyah.Shared.Configurations;

namespace Thmanyah.Discovery.Infrastructure
{
    public static class DiscoveryModule
    {
        public static IServiceCollection AddDiscoveryModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DiscoveryDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<DiscoveryService>();
            services.AddSingleton<DiscoveryEventSubscriber>();

            services.AddScoped<IDiscoveryService>(sp =>
            {
                var inner = sp.GetRequiredService<DiscoveryService>();
                var cache = sp.GetRequiredService<IDistributedCache>();
                var discoveryCacheConfiguration = configuration.GetSection("DiscoveryCacheConfiguration").Get<DiscoveryCacheConfiguration>();
                return new CachedDiscoveryService(inner, cache, discoveryCacheConfiguration);
            });

            return services;
        }
    }
}
