using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Thmanyah.Cms.Infrastructure;
using Thmanyah.Cms.Services;
using ProgramEntity = Thmanyah.Shared.Domain.Program;
using EpisodeEntity = Thmanyah.Shared.Domain.Episode;
using Microsoft.AspNetCore.Authorization;
using Thmanyah.Shared.Domain;

namespace Thmanyah.Api.Controllers
{
    /// <summary>
    /// CMS module controller for program and episode management.
    /// </summary>
    [ApiController]
    [Route("api/cms")]
    //[Authorize(Roles = "sysAdmin,contentManager")]
    public class CmsController : ControllerBase
    {
        private readonly IProgramRepository _programs;
        private readonly IEpisodeRepository _episodes;
        private readonly FileUploadService _fileUploadService;
        private readonly EpisodeStorageFactory _storageFactory;
        private readonly EpisodeValidationService _validationService;
        private readonly Thmanyah.Api.Services.ICurrentUserService _currentUser;

        public CmsController(
            IProgramRepository programs,
            IEpisodeRepository episodes,
            FileUploadService fileUploadService,
            EpisodeStorageFactory storageFactory,
            EpisodeValidationService validationService,
            Thmanyah.Api.Services.ICurrentUserService currentUser)
        {
            _programs = programs;
            _episodes = episodes;
            _fileUploadService = fileUploadService;
            _storageFactory = storageFactory;
            _validationService = validationService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Create a new program.
        /// </summary>
        [HttpPost("programs")]
        public async Task<IActionResult> CreateProgram([FromBody] CreateProgramRequest req)
        {
            var program = ProgramEntity.Create(req.Title, req.Description, req.Type, req.PresenterName, createdBy: _currentUser.UserName);
            await _programs.AddAsync(program);

            return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
        }

        /// <summary>
        /// Get program by ID.
        /// </summary>
        [HttpGet("programs/{id:guid}")]
        public async Task<IActionResult> GetProgram([FromRoute] Guid id)
        {
            var p = await _programs.GetByIdAsync(id);
            if (p == null) return NotFound();
            return Ok(p);
        }

        /// <summary>
        /// Update program (optimistic concurrency via RowVersion).
        /// </summary>
        [HttpPut("programs/{id:guid}")]
        public async Task<IActionResult> UpdateProgram([FromRoute] Guid id, [FromBody] UpdateProgramRequest req)
        {
            var existing = await _programs.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Update(req.Title, req.Description, req.Type, req.PresenterName, req.Thumbnail, updatedBy: _currentUser.UserName);

            try
            {
                await _programs.UpdateAsync(existing);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Concurrency conflict - program was modified by another user." });
            }
        }

        /// <summary>
        /// Upload thumbnail image for program.
        /// </summary>
        [HttpPost("programs/{id:guid}/thumbnail")]
        public async Task<IActionResult> UploadProgramThumbnail([FromRoute] Guid id, IFormFile file)
        {
            var program = await _programs.GetByIdAsync(id);
            if (program == null) return NotFound();

            try
            {
                string relativePath = await _fileUploadService.UploadAsync(file, "program-thumbnails");
                program.Thumbnail = relativePath;
                program.UpdatedBy = _currentUser.UserName;
                await _programs.UpdateAsync(program);

                return Ok(new { message = "Thumbnail uploaded successfully", path = relativePath });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to upload thumbnail", details = ex.Message });
            }
        }

        /// <summary>
        /// Create a new episode.
        /// </summary>
        [HttpPost("episodes")]
        public async Task<IActionResult> CreateEpisode([FromBody] CreateEpisodeRequest req)
        {
            var episode = EpisodeEntity.Create(
                req.ProgramId, req.Title, req.Description,
                req.Genre, req.Language, req.DurationInMinutes, req.PublishDate,
                createdBy: _currentUser.UserName);

            await _episodes.AddAsync(episode);

            return CreatedAtAction(nameof(GetEpisode), new { id = episode.Id }, episode);
        }

        /// <summary>
        /// Get episode by ID.
        /// </summary>
        [HttpGet("episodes/{id:guid}")]
        public async Task<IActionResult> GetEpisode([FromRoute] Guid id)
        {
            var e = await _episodes.GetByIdAsync(id);
            if (e == null) return NotFound();
            return Ok(e);
        }

        /// <summary>
        /// Update episode (optimistic concurrency via RowVersion).
        /// </summary>
        [HttpPut("episodes/{id:guid}")]
        public async Task<IActionResult> UpdateEpisode([FromRoute] Guid id, [FromBody] UpdateEpisodeRequest req)
        {
            var existing = await _episodes.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Update(req.Title, req.Description, req.Genre, req.Language,
                req.DurationInMinutes, req.PublishDate, req.Thumbnail, req.EpisodeUrl, req.StorageType,
                updatedBy: _currentUser.UserName);

            try
            {
                await _episodes.UpdateAsync(existing);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Concurrency conflict - episode was modified by another user." });
            }
        }

        /// <summary>
        /// Upload thumbnail image for episode.
        /// </summary>
        [HttpPost("episodes/{id:guid}/thumbnail")]
        public async Task<IActionResult> UploadEpisodeThumbnail([FromRoute] Guid id, IFormFile file)
        {
            var episode = await _episodes.GetByIdAsync(id);
            if (episode == null) return NotFound();

            try
            {
                string relativePath = await _fileUploadService.UploadAsync(file, "episode-thumbnails");
                episode.Thumbnail = relativePath;
                episode.UpdatedBy = _currentUser.UserName;
                await _episodes.UpdateAsync(episode);

                return Ok(new { message = "Thumbnail uploaded successfully", path = relativePath });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to upload thumbnail", details = ex.Message });
            }
        }

        /// <summary>
        /// Upload episode content using specified storage strategy.
        /// Validates that content is not a static URL before storing.
        /// </summary>
        [HttpPost("episodes/{id:guid}/content")]
        public async Task<IActionResult> UploadEpisodeContent(
            [FromRoute] Guid id,
            IFormFile? file = null)
        {
            var episode = await _episodes.GetByIdAsync(id);
            if (episode == null) return NotFound();

            try
            {
                // Determine storage strategy from episode entity, default to LocalFile
                var storageStrategy = episode.StorageType ?? Thmanyah.Shared.Domain.EpisodeStorageType.LocalFile;
                IEpisodeStorageStrategy strategy = _storageFactory.GetStrategy(storageStrategy);
                
                if (strategy.StorageType == EpisodeStorageType.ExternalUrl && file == null)
                {
                    // For external URL strategy, get URL from query or form
                    string externalUrl = HttpContext.Request.Form["externalUrl"].ToString();
                    if (string.IsNullOrEmpty(externalUrl))
                        return BadRequest(new { error = "External URL required for ExternalUrl strategy" });

                    // Validate it's an external URL, not a static arbitrary URL
                    if (_validationService.IsStaticUrl(externalUrl))
                        return BadRequest(new { error = "Static/arbitrary URLs are not allowed. Use managed storage or known video platforms (YouTube, Vimeo, etc.)" });

                    var extUrlStrategy = (ExternalUrlStrategy)strategy;
                    episode.EpisodeUrl = await extUrlStrategy.SetExternalUrl(externalUrl);
                }
                else
                {
                    if (file == null || file.Length == 0)
                        return BadRequest(new { error = "File is required for this storage strategy" });

                    episode.EpisodeUrl = await strategy.StoreAsync(episode.Id, file.OpenReadStream(), file.FileName);
                }

                episode.StorageType = strategy.StorageType;
                episode.UpdatedBy = _currentUser.UserName;
                await _episodes.UpdateAsync(episode);

                return Ok(new
                {
                    message = "Episode content uploaded successfully",
                    storageType = strategy.StorageType,
                    episodeUrl = episode.EpisodeUrl
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to upload episode content", details = ex.Message });
            }
        }

        /// <summary>
        /// Stream episode content by ID.
        /// Validates that the episode has managed storage before streaming.
        /// </summary>
        [HttpGet("episodes/{id:guid}/stream")]
        public async Task<IActionResult> StreamEpisode([FromRoute] Guid id)
        {
            var episode = await _episodes.GetByIdAsync(id);
            if (episode == null) return NotFound();

            if (string.IsNullOrEmpty(episode.EpisodeUrl))
                return BadRequest(new { error = "Episode has no content" });

            try
            {
                // Validate that episode has a storage type
                if (!episode.StorageType.HasValue)
                {
                    return BadRequest(new { error = "Episode storage type is invalid" });
                }

                var storageType = episode.StorageType.Value;

                // Check if it's valid managed storage
                bool isValid = await _validationService.IsValidManagedStorageAsync(episode.EpisodeUrl, storageType);
                if (!isValid)
                    return BadRequest(new { error = "Episode URL is not valid managed storage" });

                // For external URLs, return redirect
                if (storageType == EpisodeStorageType.ExternalUrl)
                {
                    return Redirect(episode.EpisodeUrl);
                }

                // For managed storage, get content stream
                IEpisodeStorageStrategy strategy = _storageFactory.GetStrategy(storageType);
                var stream = await strategy.RetrieveAsync(episode.EpisodeUrl);

                // Return video file with appropriate content type
                return File(stream, "video/mp4", $"episode-{episode.Id}.mp4");
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, new { error = "Streaming from this storage type is not yet implemented" });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to stream episode", details = ex.Message });
            }
        }

        /// <summary>
        /// Download episode content by ID with custom filename.
        /// </summary>
        [HttpGet("episodes/{id:guid}/download")]
        public async Task<IActionResult> DownloadEpisode([FromRoute] Guid id, [FromQuery] string? filename = null)
        {
            var episode = await _episodes.GetByIdAsync(id);
            if (episode == null) return NotFound();

            if (string.IsNullOrEmpty(episode.EpisodeUrl))
                return BadRequest(new { error = "Episode has no content" });

            try
            {
                // Validate storage type
                if (!episode.StorageType.HasValue)
                {
                    return BadRequest(new { error = "Episode storage type is invalid" });
                }

                var storageType = episode.StorageType.Value;

                // For external URLs, redirect to them
                if (storageType == EpisodeStorageType.ExternalUrl)
                {
                    return Redirect(episode.EpisodeUrl);
                }

                // Validate managed storage
                bool isValid = await _validationService.IsValidManagedStorageAsync(episode.EpisodeUrl, storageType);
                if (!isValid)
                    return BadRequest(new { error = "Episode URL is not valid managed storage" });

                // Get content stream
                IEpisodeStorageStrategy strategy = _storageFactory.GetStrategy(storageType);
                var stream = await strategy.RetrieveAsync(episode.EpisodeUrl);

                string downloadFilename = filename ?? $"{episode.Title}.mp4";
                return File(stream, "application/octet-stream", downloadFilename);
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, new { error = "Download from this storage type is not yet implemented" });
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to download episode", details = ex.Message });
            }
        }

        /// <summary>
        /// Get available storage strategies.
        /// </summary>
        [HttpGet("storage-strategies")]
        public IActionResult GetAvailableStorageStrategies()
        {
            var strategies = _storageFactory.GetAvailableStrategies();
            var strategyNames = strategies.Select(s => new { type = s, name = s.ToString() });
            return Ok(new { availableStrategies = strategyNames });
        }
    }

    // Request/Response DTOs
    public class CreateProgramRequest
    {
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public ProgramType Type { get; set; } = ProgramType.Other;
        public string? PresenterName { get; set; }
    }

    public class UpdateProgramRequest
    {
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public ProgramType Type { get; set; } = ProgramType.Other;
        public string? PresenterName { get; set; }
        public string? Thumbnail { get; set; }
        public byte[]? RowVersion { get; set; }
    }

    public class CreateEpisodeRequest
    {
        public Guid ProgramId { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public EpisodeGenre Genre { get; set; } = EpisodeGenre.Other;
        public EpisodeLanguage Language { get; set; } = EpisodeLanguage.English;
        public int DurationInMinutes { get; set; }
        public DateTime? PublishDate { get; set; }
    }

    public class UpdateEpisodeRequest
    {
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public EpisodeGenre Genre { get; set; } = EpisodeGenre.Other;
        public EpisodeLanguage Language { get; set; } = EpisodeLanguage.English;
        public int DurationInMinutes { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? Thumbnail { get; set; }
        public string? EpisodeUrl { get; set; }
        public Thmanyah.Shared.Domain.EpisodeStorageType? StorageType { get; set; }
        public byte[]? RowVersion { get; set; }
    }
}
