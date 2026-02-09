using Thmanyah.Shared.Domain;
using Thmanyah.Shared.Services;

namespace Thmanyah.Cms.Infrastructure
{
    public interface IEpisodeRepository
    {
        Task<Episode?> GetByIdAsync(Guid id);
        Task AddAsync(Episode episode);
        Task UpdateAsync(Episode episode);
        Task SaveChangesAsync();
    }

    public class EpisodeRepository : IEpisodeRepository
    {
        private readonly CmsDbContext _db;
        private readonly IDomainEventPublisher? _publisher;

        public EpisodeRepository(CmsDbContext db, IDomainEventPublisher? publisher = null)
        {
            _db = db;
            _publisher = publisher;
        }

        public async Task<Episode?> GetByIdAsync(Guid id)
        {
            return await _db.Episodes.FindAsync(id);
        }


        public async Task AddAsync(Episode episode)
        {
            _db.Episodes.Add(episode);
            await SaveChangesAsync();
            await PublishEpisodeChangedAsync(episode);
        }

        public async Task UpdateAsync(Episode episode)
        {
            _db.Episodes.Update(episode);
            await SaveChangesAsync();
            await PublishEpisodeChangedAsync(episode);
        }

        private async Task PublishEpisodeChangedAsync(Episode episode)
        {
            if (_publisher == null) return;

            var evt = new Events.EpisodeChangedEvent
            {
                Id = episode.Id,
                ProgramId = episode.ProgramId,
                Title = episode.Title,
                Description = episode.Description,
                Genre = (int)episode.Genre,
                Language = (int)episode.Language,
                DurationInMinutes = episode.DurationInMinutes,
                PublishDate = episode.PublishDate,
                Thumbnail = episode.Thumbnail,
                EpisodeUrl = episode.EpisodeUrl,
                StorageType = episode.StorageType == null ? (int?)null : (int)episode.StorageType,
                UpdatedAt = episode.UpdatedAt
            };

            await _publisher.PublishAsync(evt);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
