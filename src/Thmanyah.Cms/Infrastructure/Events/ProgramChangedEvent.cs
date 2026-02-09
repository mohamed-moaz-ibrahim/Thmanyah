using System;

namespace Thmanyah.Cms.Infrastructure.Events
{
    public class ProgramChangedEvent
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PresenterName { get; set; }
        public string? Thumbnail { get; set; }
        public int Type { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
