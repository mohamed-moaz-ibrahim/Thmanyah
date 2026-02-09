using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Thmanyah.Shared.Domain;
using Thmanyah.Shared.Configurations;

namespace Thmanyah.Cms.Services
{
    /// <summary>
    /// Local file system storage strategy for episode content.
    /// Stores videos in a local directory and returns file system paths.
    /// </summary>
    public class LocalStorageStrategy : IEpisodeStorageStrategy
    {
        private readonly string _storagePath;

        public EpisodeStorageType StorageType => EpisodeStorageType.LocalFile;

        public LocalStorageStrategy(IOptions<StorageConfiguration> storageConfig)
        {
            // Get storage path from configuration or use default
            _storagePath = storageConfig.Value.Local.Path ?? "wwwroot\\episodes";
            
            // Ensure storage directory exists
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        public async Task<string> StoreAsync(Guid episodeId, Stream contentStream, string fileName)
        {
            if (contentStream == null || !contentStream.CanRead)
                throw new ArgumentException("Invalid content stream", nameof(contentStream));

            // Create episode-specific subdirectory
            string episodePath = Path.Combine(_storagePath, episodeId.ToString());
            if (!Directory.Exists(episodePath))
            {
                Directory.CreateDirectory(episodePath);
            }

            // Generate unique filename to avoid conflicts
            string sanitizedFileName = Path.GetFileName(fileName);
            string fileWithTimestamp = $"{Path.GetFileNameWithoutExtension(sanitizedFileName)}-{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(sanitizedFileName)}";
            string filePath = Path.Combine(episodePath, fileWithTimestamp);

            // Write stream to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                contentStream.Seek(0, SeekOrigin.Begin);
                await contentStream.CopyToAsync(fileStream);
            }

            // Return relative path for storage
            return Path.Combine("episodes", episodeId.ToString(), fileWithTimestamp).Replace("\\", "/");
        }

        public async Task<Stream> RetrieveAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Invalid reference", nameof(reference));

            // Convert relative path to absolute path
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", reference.Replace("/", "\\"));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Episode file not found: {reference}");

            // Return file stream
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return await Task.FromResult((System.IO.Stream)stream);
        }

        public async Task DeleteAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Invalid reference", nameof(reference));

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", reference.Replace("/", "\\"));
            //string filePath = Path.Combine(_storagePath, reference.Replace("/", "\\"));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Clean up empty episode directory
            string episodeDir = Path.GetDirectoryName(filePath);
            if (Directory.Exists(episodeDir) && Directory.GetFiles(episodeDir).Length == 0)
            {
                Directory.Delete(episodeDir);
            }

            await Task.CompletedTask;
        }

        public async Task<bool> ValidateAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return await Task.FromResult(false);

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", reference.Replace("/", "\\"));
            //string filePath = Path.Combine(_storagePath, reference.Replace("/", "\\"));
            return await Task.FromResult(File.Exists(filePath));
        }
    }
}
