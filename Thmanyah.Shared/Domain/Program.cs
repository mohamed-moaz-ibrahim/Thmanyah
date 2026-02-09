using System;
using System.ComponentModel.DataAnnotations;

namespace Thmanyah.Shared.Domain
{
    /// <summary>
    /// Program type enumeration.
    /// </summary>
    public enum ProgramType
    {
        Podcast = 0,
        Documentary = 1,
        Other = 2
    }

    /// <summary>
    /// Program entity for storing program information.
    /// </summary>
    public class Program
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        public ProgramType Type { get; set; } = ProgramType.Other;

        public string? PresenterName { get; set; }

        public string? Thumbnail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // Auditing
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Soft delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        public static Program Create(string title, string? description, 
            ProgramType type = ProgramType.Other, string? presenterName = null, string? thumbnail = null,
            string? createdBy = null)
        {
            var program = new Program
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                Type = type,
                PresenterName = presenterName,
                Thumbnail = thumbnail,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return program;
        }

        public void Update(string title, string? description, 
            ProgramType type = ProgramType.Other, string? presenterName = null, string? thumbnail = null,
            string? updatedBy = null)
        {
            if (Title == title && Description == description && Type == type && 
                PresenterName == presenterName && Thumbnail == thumbnail) return;

            Title = title;
            Description = description;
            Type = type;
            PresenterName = presenterName;
            Thumbnail = thumbnail;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete(string? deletedBy = null)
        {
            if (IsDeleted) return;
            IsDeleted = true;
            DeletedBy = deletedBy;
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = deletedBy;
        }
    }
}
