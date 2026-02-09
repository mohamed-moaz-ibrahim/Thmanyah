using System.Threading.Tasks;
using Thmanyah.Cms.Infrastructure;
using Thmanyah.Cms.Infrastructure.Events;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Thmanyah.Shared.Services;

namespace Thmanyah.Discovery.Services
{
    /// <summary>
    /// Subscribes to CMS domain events and bumps discovery cache version to invalidate lists.
    /// This is a lightweight CQRS pattern: writers publish events, discovery reads from cache.
    /// </summary>
    public class DiscoveryEventSubscriber
    {
        private readonly IDomainEventPublisher _publisher;
        private readonly IDistributedCache _cache;

        public DiscoveryEventSubscriber(IDomainEventPublisher publisher, IDistributedCache cache)
        {
            _publisher = publisher;
            _cache = cache;

            _publisher.Subscribe<ProgramChangedEvent>(OnProgramChangedAsync);
            _publisher.Subscribe<EpisodeChangedEvent>(OnEpisodeChangedAsync);
        }

        private async Task OnProgramChangedAsync(ProgramChangedEvent evt)
        {
            await BumpVersionAsync();
        }

        private async Task OnEpisodeChangedAsync(EpisodeChangedEvent evt)
        {
            await BumpVersionAsync();
        }

        private async Task BumpVersionAsync()
        {
            var key = "discovery:version";
            var current = await _cache.GetStringAsync(key) ?? "0";
            if (!int.TryParse(current, out var v)) v = 0;
            v++;
            await _cache.SetStringAsync(key, v.ToString());
        }
    }
}
