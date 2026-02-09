using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Thmanyah.Cms.Infrastructure;
using Thmanyah.Cms.Services;
using Thmanyah.Shared.Configurations;
using Thmanyah.Shared.Services;

namespace Thmanyah.Cms.Infrastructure
{
    public static class CmsModule
    {
        public static IServiceCollection AddCmsModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<CmsDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IProgramRepository, ProgramRepository>();
            services.AddScoped<IEpisodeRepository, EpisodeRepository>();
            services.AddSingleton<IDomainEventPublisher, InMemoryEventPublisher>();
            services.AddScoped<FileUploadService>();
            services.AddSingleton<LocalStorageStrategy>();
            services.AddSingleton<AzureStorageStrategy>();
            services.AddSingleton<AWSStorageStrategy>();
            services.AddSingleton<ExternalUrlStrategy>();
            services.AddSingleton<EpisodeStorageFactory>();
            services.AddScoped<EpisodeValidationService>();

            return services;
        }
    }
}
