using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Thmanyah.Shared.Domain;
using Microsoft.Extensions.Options;
using Thmanyah.Shared.Configurations;

namespace Thmanyah.Cms.Services
{
    /// <summary>
    /// AWS S3 storage strategy for episode content.
    /// Stores videos in Amazon S3 and returns secure URLs.
    /// </summary>
    public class AWSStorageStrategy : IEpisodeStorageStrategy
    {
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _region;
        private readonly string _bucketName;

        public EpisodeStorageType StorageType => EpisodeStorageType.AWSStorage;

        public AWSStorageStrategy(IOptions<StorageConfiguration> storageConfig)
        {
            _accessKey = storageConfig.Value.AWSConfiguration.AccessKey;
            _secretKey = storageConfig.Value.AWSConfiguration.SecretKey;
            _region = storageConfig.Value.AWSConfiguration.Region;
            _bucketName = storageConfig.Value.AWSConfiguration.BucketName;
        }

        public async Task<string> StoreAsync(Guid episodeId, Stream contentStream, string fileName)
        {
            if (contentStream == null || !contentStream.CanRead)
                throw new ArgumentException("Invalid content stream", nameof(contentStream));

            var creds = new BasicAWSCredentials(_accessKey, _secretKey);
            var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(_region) };
            using var s3 = new AmazonS3Client(creds, config);

            string sanitizedFileName = Path.GetFileName(fileName);
            string fileWithTimestamp = $"{Path.GetFileNameWithoutExtension(sanitizedFileName)}-{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(sanitizedFileName)}";
            string key = $"episodes/{episodeId}/{fileWithTimestamp}";

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = contentStream,
                ContentType = "application/octet-stream"
            };

            await s3.PutObjectAsync(putRequest);

            var s3Url = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}";
            return s3Url;
        }

        public async Task<Stream> RetrieveAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Invalid reference", nameof(reference));

            string key = reference;
            if (Uri.TryCreate(reference, UriKind.Absolute, out var uri) && uri.Host.Contains("amazonaws.com", StringComparison.OrdinalIgnoreCase))
            {
                key = uri.LocalPath.TrimStart('/');
            }

            var creds = new BasicAWSCredentials(_accessKey, _secretKey);
            var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(_region) };
            using var s3 = new AmazonS3Client(creds, config);

            var resp = await s3.GetObjectAsync(new GetObjectRequest { BucketName = _bucketName, Key = key });
            var ms = new MemoryStream();
            await resp.ResponseStream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        public async Task DeleteAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                throw new ArgumentException("Invalid reference", nameof(reference));

            string key = reference;
            if (Uri.TryCreate(reference, UriKind.Absolute, out var uri) && uri.Host.Contains("amazonaws.com", StringComparison.OrdinalIgnoreCase))
            {
                key = uri.LocalPath.TrimStart('/');
            }

            var creds = new BasicAWSCredentials(_accessKey, _secretKey);
            var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(_region) };
            using var s3 = new AmazonS3Client(creds, config);

            await s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucketName, Key = key });
        }

        public async Task<bool> ValidateAsync(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return await Task.FromResult(false);

            string key = reference;
            if (Uri.TryCreate(reference, UriKind.Absolute, out var uri) && uri.Host.Contains("amazonaws.com", StringComparison.OrdinalIgnoreCase))
            {
                key = uri.LocalPath.TrimStart('/');
            }

            var creds = new BasicAWSCredentials(_accessKey, _secretKey);
            var config = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(_region) };
            using var s3 = new AmazonS3Client(creds, config);

            try
            {
                var metadata = await s3.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = _bucketName, Key = key });
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
