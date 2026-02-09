using System;
using System.Threading.Tasks;
using Thmanyah.Shared.Domain;

namespace Thmanyah.Cms.Services
{
    /// <summary>
    /// Service for validating episode content before upload/streaming.
    /// Ensures episodes are not static URLs and are properly managed.
    /// </summary>
    public class EpisodeValidationService
    {
        /// <summary>
        /// Validates that an episode URL is managed by our storage system (not static).
        /// </summary>
        /// <param name="episodeUrl">The episode URL to validate</param>
        /// <param name="storageType">The storage type of the episode</param>
        /// <returns>True if valid managed storage, false if static URL</returns>
        public async Task<bool> IsValidManagedStorageAsync(string episodeUrl, EpisodeStorageType storageType)
        {
            if (string.IsNullOrEmpty(episodeUrl))
                return false;

            // ExternalUrl storage type is allowed as it's explicitly managed
            if (storageType == EpisodeStorageType.ExternalUrl)
            {
                return await IsValidExternalUrlAsync(episodeUrl);
            }

            // For managed storage (Local, Azure, AWS), validate the URL format
            return storageType switch
            {
                EpisodeStorageType.LocalFile => IsValidLocalPath(episodeUrl),
                EpisodeStorageType.AzureStorage => await IsValidAzureUrlAsync(episodeUrl),
                EpisodeStorageType.AWSStorage => await IsValidAwsUrlAsync(episodeUrl),
                _ => false
            };
        }

        /// <summary>
        /// Checks if URL is a static/arbitrary URL (not managed by us).
        /// </summary>
        public bool IsStaticUrl(string url)
        {
            // Static URLs are arbitrary URLs not in our managed storage paths
            if (string.IsNullOrEmpty(url))
                return false;

            // If it's a local path - managed
            if (url.StartsWith("episodes/", StringComparison.OrdinalIgnoreCase) || 
                url.StartsWith("/episodes/", StringComparison.OrdinalIgnoreCase))
                return false;

            // If it's Azure blob storage - managed
            if (url.Contains(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
                return false;

            // If it's AWS S3 - managed
            if (url.Contains(".s3.", StringComparison.OrdinalIgnoreCase) && 
                url.Contains(".amazonaws.com", StringComparison.OrdinalIgnoreCase))
                return false;

            // Any other URL is considered static/arbitrary
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates local file path format.
        /// </summary>
        private bool IsValidLocalPath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   (path.StartsWith("episodes/", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/episodes/", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validates Azure Blob Storage URL format.
        /// </summary>
        private async Task<bool> IsValidAzureUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            bool isValid = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                          url.Contains(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase) &&
                          url.Contains("/episodes/", StringComparison.OrdinalIgnoreCase);

            return await Task.FromResult(isValid);
        }

        /// <summary>
        /// Validates AWS S3 URL format.
        /// </summary>
        private async Task<bool> IsValidAwsUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            bool isValid = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                          url.Contains(".s3.", StringComparison.OrdinalIgnoreCase) &&
                          url.Contains(".amazonaws.com", StringComparison.OrdinalIgnoreCase) &&
                          url.Contains("/episodes/", StringComparison.OrdinalIgnoreCase);

            return await Task.FromResult(isValid);
        }

        /// <summary>
        /// Validates external URL (YouTube, Vimeo, etc.).
        /// </summary>
        private async Task<bool> IsValidExternalUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            // Validate URL format
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            // Check if it's a known video platform
            string host = uri.Host.ToLowerInvariant();
            return await Task.FromResult(
                host.Contains("youtube.com") ||
                host.Contains("youtu.be") ||
                host.Contains("vimeo.com") ||
                host.Contains("dailymotion.com") ||
                host.Contains("twitch.tv"));
        }
    }
}
