using Thmanyah.Shared.Domain;
using Thmanyah.Shared.Services;

namespace Thmanyah.Cms.Infrastructure
{
    public interface IProgramRepository
    {
        Task<Program?> GetByIdAsync(Guid id);
        Task AddAsync(Program program);
        Task UpdateAsync(Program program);
        Task SaveChangesAsync();
    }

    public class ProgramRepository : IProgramRepository
    {
        private readonly CmsDbContext _db;
        private readonly IDomainEventPublisher? _publisher;

        public ProgramRepository(CmsDbContext db, IDomainEventPublisher? publisher = null)
        {
            _db = db;
            _publisher = publisher;
        }

        public async Task<Program?> GetByIdAsync(Guid id)
        {
            return await _db.Programs.FindAsync(id);
        }


        public async Task AddAsync(Program program)
        {
            _db.Programs.Add(program);
            await SaveChangesAsync();
            await PublishProgramChangedAsync(program);
        }

        public async Task UpdateAsync(Program program)
        {
            _db.Programs.Update(program);
            await SaveChangesAsync();
            await PublishProgramChangedAsync(program);
        }

        private async Task PublishProgramChangedAsync(Program program)
        {
            if (_publisher == null) return;

            var evt = new Events.ProgramChangedEvent
            {
                Id = program.Id,
                Title = program.Title,
                Description = program.Description,
                PresenterName = program.PresenterName,
                Thumbnail = program.Thumbnail,
                Type = (int)program.Type,
                UpdatedAt = program.UpdatedAt
            };

            await _publisher.PublishAsync(evt);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
