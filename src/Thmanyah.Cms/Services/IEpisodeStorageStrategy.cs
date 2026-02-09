using System.Threading.Tasks;
using Thmanyah.Shared.Domain;

namespace Thmanyah.Cms.Services
{
    /// <summary>
    /// Strategy interface for storing episode content (videos/media).
    /// Implements polymorphism to support multiple storage backends.
    /// </summary>
    public interface IEpisodeStorageStrategy
    {
        /// <summary>
        /// Gets the type of this storage strategy.
        /// </summary>
        EpisodeStorageType StorageType { get; }

        /// <summary>
        /// Stores episode content and returns a reference (URL or path).
        /// </summary>
        /// <param name="episodeId">The episode identifier</param>
        /// <param name="contentStream">The video/media stream to store</param>
        /// <param name="fileName">Original file name</param>
        /// <returns>Stored reference (URL or file path)</returns>
        Task<string> StoreAsync(System.Guid episodeId, System.IO.Stream contentStream, string fileName);

        /// <summary>
        /// Retrieves episode content by reference.
        /// </summary>
        /// <param name="reference">The URL or file path reference returned by StoreAsync</param>
        /// <returns>Content stream</returns>
        Task<System.IO.Stream> RetrieveAsync(string reference);

        /// <summary>
        /// Deletes episode content.
        /// </summary>
        /// <param name="reference">The URL or file path reference returned by StoreAsync</param>
        Task DeleteAsync(string reference);

        /// <summary>
        /// Validates if the reference is accessible/valid for this strategy.
        /// </summary>
        /// <param name="reference">The URL or file path reference</param>
        /// <returns>True if valid and accessible</returns>
        Task<bool> ValidateAsync(string reference);
    }
}
