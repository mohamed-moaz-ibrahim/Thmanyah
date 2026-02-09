using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Thmanyah.Cms.Services
{
    /// <summary>
    /// Service for handling file uploads (thumbnails, images).
    /// Provides validation, storage, and retrieval functionality.
    /// </summary>
    public class FileUploadService
    {
        private readonly string _uploadPath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedMimeTypes;
        private readonly string[] _allowedExtensions;

        public FileUploadService(
            string uploadPath = "wwwroot/uploads",
            long maxFileSize = 10 * 1024 * 1024, // 10 MB
            string[]? allowedMimeTypes = null,
            string[]? allowedExtensions = null)
        {
            _uploadPath = uploadPath;
            _maxFileSize = maxFileSize;

            // Default allowed image types
            _allowedMimeTypes = allowedMimeTypes ?? new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp",
                "image/gif"
            };

            _allowedExtensions = allowedExtensions ?? new[]
            {
                ".jpg", ".jpeg", ".png", ".webp", ".gif"
            };

            // Ensure upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        /// <summary>
        /// Upload a file and return its relative path.
        /// </summary>
        public async Task<string> UploadAsync(IFormFile file, string category = "general")
        {
            // Validate file
            ValidateFile(file);

            // Create category subdirectory
            string categoryPath = Path.Combine(_uploadPath, category);
            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }

            // Generate unique filename
            string sanitizedFileName = Path.GetFileName(file.FileName);
            string fileWithTimestamp = $"{Path.GetFileNameWithoutExtension(sanitizedFileName)}-{Guid.NewGuid():N}{Path.GetExtension(sanitizedFileName)}";
            string filePath = Path.Combine(categoryPath, fileWithTimestamp);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for storage
            return Path.Combine("uploads", category, fileWithTimestamp).Replace("\\", "/");
        }

        /// <summary>
        /// Delete an uploaded file.
        /// </summary>
        public async Task DeleteAsync(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("Path cannot be empty", nameof(relativePath));

            string fullPath = Path.Combine(_uploadPath, relativePath.Replace("/", "\\"));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Get file stream for download/serving.
        /// </summary>
        public async Task<Stream> GetFileAsync(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("Path cannot be empty", nameof(relativePath));

            string fullPath = Path.Combine(_uploadPath, relativePath.Replace("/", "\\"));

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {relativePath}");

            return await Task.FromResult(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
        }

        /// <summary>
        /// Validate uploaded file.
        /// </summary>
        private void ValidateFile(IFormFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file), "File cannot be null");

            if (file.Length == 0)
                throw new ArgumentException("File is empty", nameof(file));

            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)} MB", nameof(file));

            // Validate MIME type
            if (!_allowedMimeTypes.Contains(file.ContentType))
                throw new ArgumentException($"File type '{file.ContentType}' is not allowed", nameof(file));

            // Validate extension
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                throw new ArgumentException($"File extension '{extension}' is not allowed", nameof(file));
        }
    }
}
