using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Thmanyah.Shared.Domain;
using Microsoft.Extensions.Options;
using Thmanyah.Shared.Configurations;

namespace Thmanyah.Cms.Services
{
    /// <summary>
    /// Azure Blob Storage strategy for episode content.
    /// Stores videos in Azure Blob Storage and returns secure URLs.
    /// </summary>
    public class AzureStorageStrategy : IEpisodeStorageStrategy
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public EpisodeStorageType StorageType => EpisodeStorageType.AzureStorage;

        public AzureStorageStrategy(IOptions<StorageConfiguration> storageConfig)
        {
            _connectionString = storageConfig.Value.AzureConfiguration.ConnectionString;
            _containerName = storageConfig.Value.AzureConfiguration.ContainerName ?? "episodes";
        }

        public async Task<string> StoreAsync(Guid episodeId, Stream contentStream, string fileName)
        {
            if (contentStream == null || !contentStream.CanRead)
                throw new ArgumentException("Invalid content stream", nameof(contentStream));

            var containerClient = new BlobContainerClient(_connectionString, _containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            string sanitizedFileName = Path.GetFileName(fileName);
            string fileWithTimestamp = $"{Path.GetFileNameWithoutExtension(sanitizedFileName)}-{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(sanitizedFileName)}";
            string blobPath = $"episodes/{episodeId}/{fileWithTimestamp}";

            var blobClient = containerClient.GetBlobClient(blobPath);
            contentStream.Seek(0, SeekOrigin.Begin);
            await blobClient.UploadAsync(contentStream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        public async Task<Stream> RetrieveAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Invalid reference", nameof(reference));

            BlobClient blobClient;
            if (Uri.TryCreate(reference, UriKind.Absolute, out var uri) && uri.Host.Contains("blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                blobClient = new BlobClient(uri);
            }
            else
            {
                var containerClient = new BlobContainerClient(_connectionString, _containerName);
                blobClient = containerClient.GetBlobClient(reference);
            }

            var ms = new MemoryStream();
            await blobClient.DownloadToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        public async Task DeleteAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Invalid reference", nameof(reference));

            BlobClient blobClient;
            if (Uri.TryCreate(reference, UriKind.Absolute, out var uri) && uri.Host.Contains("blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                blobClient = new BlobClient(uri);
            }
            else
            {
                var containerClient = new BlobContainerClient(_connectionString, _containerName);
                blobClient = containerClient.GetBlobClient(reference);
            }

            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<bool> ValidateAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return await Task.FromResult(false);

            BlobClient blobClient;
            if (Uri.TryCreate(reference, UriKind.Absolute, out var uri) && uri.Host.Contains("blob.core.windows.net", StringComparison.OrdinalIgnoreCase))
            {
                blobClient = new BlobClient(uri);
            }
            else
            {
                var containerClient = new BlobContainerClient(_connectionString, _containerName);
                blobClient = containerClient.GetBlobClient(reference);
            }

            var exists = await blobClient.ExistsAsync();
            return exists.Value;
        }
    }
}

