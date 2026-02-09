using System;
using System.IO;
using System.Threading.Tasks;
using Thmanyah.Shared.Domain;

namespace Thmanyah.Cms.Services
{
    /// <summary>
    /// External URL reference strategy for episode content.
    /// Simply stores external URLs (e.g., YouTube, Vimeo) without managing storage.
    /// </summary>
    public class ExternalUrlStrategy : IEpisodeStorageStrategy
    {
        public EpisodeStorageType StorageType => EpisodeStorageType.ExternalUrl;

        public async Task<string> StoreAsync(Guid episodeId, Stream contentStream, string fileName)
        {
            // This strategy doesn't accept streams - only URLs
            // In production, validate the URL format
            throw new NotSupportedException("ExternalUrlStrategy requires URL reference, not file stream. Use SetExternalUrl() instead.");
        }

        /// <summary>
        /// Sets an external URL reference directly.
        /// </summary>
        /// <param name="externalUrl">The external URL (YouTube, Vimeo, etc.)</param>
        /// <returns>The validated URL</returns>
        public async Task<string> SetExternalUrl(string externalUrl)
        {
            if (string.IsNullOrEmpty(externalUrl))
                throw new ArgumentException("URL cannot be empty", nameof(externalUrl));

            // Validate URL format
            if (!Uri.TryCreate(externalUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException("Invalid URL format", nameof(externalUrl));

            return await Task.FromResult(externalUrl);
        }

        public async Task<Stream> RetrieveAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Invalid reference", nameof(reference));

            // External URLs cannot be retrieved as streams directly
            // The client should handle URL redirection or embedding
            throw new NotSupportedException("ExternalUrlStrategy provides URLs for embedding/redirection only");
        }

        public async Task DeleteAsync(string reference)
        {
            // No-op for external URLs - we don't manage their storage
            await Task.CompletedTask;
        }

        public async Task<bool> ValidateAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return await Task.FromResult(false);

            // Validate URL format
            if (Uri.TryCreate(reference, UriKind.Absolute, out var uri))
            {
                // Optionally verify URL is accessible (HTTP HEAD request)
                // For now, just validate format
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }
    }
}
