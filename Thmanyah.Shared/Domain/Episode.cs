using System;
using System.ComponentModel.DataAnnotations;

namespace Thmanyah.Shared.Domain
{
    /// <summary>
    /// Episode genre enumeration.
    /// </summary>
    public enum EpisodeGenre
    {
        News = 0,
        Entertainment = 1,
        Educational = 2,
        Sports = 3,
        Music = 4,
        Comedy = 5,
        Documentary = 6,
        Drama = 7,
        Other = 8
    }

    /// <summary>
    /// Episode language enumeration.
    /// </summary>
    public enum EpisodeLanguage
    {
        English = 0,
        Arabic = 1,
        French = 2,
        Spanish = 3,
        German = 4,
        Mandarin = 5,
        Japanese = 6,
        Other = 7
    }

    /// <summary>
    /// Episode entity for storing episode information.
    /// </summary>
    public class Episode
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ProgramId { get; set; }

        [Required]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        public EpisodeGenre Genre { get; set; } = EpisodeGenre.Other;

        public EpisodeLanguage Language { get; set; } = EpisodeLanguage.English;

        public int DurationInMinutes { get; set; }

        public DateTime? PublishDate { get; set; }

        public string? Thumbnail { get; set; }

        public string? EpisodeUrl { get; set; }

        public EpisodeStorageType? StorageType { get; set; }

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

        public static Episode Create(Guid programId, string title, string? description, 
            EpisodeGenre genre = EpisodeGenre.Other, EpisodeLanguage language = EpisodeLanguage.English, 
            int durationInMinutes = 0, DateTime? publishDate = null, string? thumbnail = null,
            string? episodeUrl = null, EpisodeStorageType? storageType = null, string? createdBy = null)
        {
            var episode = new Episode
            {
                Id = Guid.NewGuid(),
                ProgramId = programId,
                Title = title,
                Description = description,
                Genre = genre,
                Language = language,
                DurationInMinutes = durationInMinutes,
                PublishDate = publishDate,
                Thumbnail = thumbnail,
                EpisodeUrl = episodeUrl,
                StorageType = storageType,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return episode;
        }

        public void Update(string title, string? description, 
            EpisodeGenre genre = EpisodeGenre.Other, EpisodeLanguage language = EpisodeLanguage.English, 
            int durationInMinutes = 0, DateTime? publishDate = null, string? thumbnail = null,
            string? episodeUrl = null, EpisodeStorageType? storageType = null, string? updatedBy = null)
        {
            if (Title == title && Description == description && Genre == genre && 
                Language == language && DurationInMinutes == durationInMinutes && 
                PublishDate == publishDate && Thumbnail == thumbnail &&
                EpisodeUrl == episodeUrl && StorageType == storageType) return;

            Title = title;
            Description = description;
            Genre = genre;
            Language = language;
            DurationInMinutes = durationInMinutes;
            PublishDate = publishDate;
            Thumbnail = thumbnail;
            EpisodeUrl = episodeUrl;
            StorageType = storageType;
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
