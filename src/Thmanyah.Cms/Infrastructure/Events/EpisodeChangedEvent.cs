using System;

namespace Thmanyah.Cms.Infrastructure.Events
{
    public class EpisodeChangedEvent
    {
        public Guid Id { get; set; }
        public Guid ProgramId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Genre { get; set; }
        public int Language { get; set; }
        public int DurationInMinutes { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? Thumbnail { get; set; }
        public string? EpisodeUrl { get; set; }
        public int? StorageType { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
