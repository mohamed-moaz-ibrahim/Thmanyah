using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Thmanyah.Discovery.Services;
using Thmanyah.Shared.Domain;

namespace Thmanyah.Api.Controllers
{
    /// <summary>
    /// Discovery module controller (read-heavy, cache-aside).
    /// Serves denormalized read models from CMS events.
    /// </summary>
    [ApiController]
    [Route("api/discovery")]
    //[Microsoft.AspNetCore.Authorization.Authorize(Roles = "user,contentManager,sysAdmin")]
    public class DiscoveryController : ControllerBase
    {
        private readonly IDiscoveryService _service;

        public DiscoveryController(IDiscoveryService service)
        {
            _service = service;
        }

        /// <summary>
        /// List programs (from read model, updated via CMS events).
        /// Supports filtering by program type and search term.
        /// </summary>
        [HttpGet("programs")]
        public async System.Threading.Tasks.Task<IActionResult> ListPrograms(
            [FromQuery] int page = 1,
            [FromQuery] int size = 20,
            [FromQuery] ProgramType? type = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? after = null)
        {
            try
            {

                var programs = await _service.ListProgramsAsync(page: page, size: size, type: type, searchTerm: searchTerm, after: after);
                return Ok(programs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// List episodes (from read model, updated via CMS events).
        /// Supports filtering by program ID, genre, language, and search term.
        /// </summary>
        [HttpGet("episodes")]
        public async System.Threading.Tasks.Task<IActionResult> ListEpisodes(
            [FromQuery] Guid? programId = null,
            [FromQuery] EpisodeGenre? genre = null,
            [FromQuery] EpisodeLanguage? language = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20,
            [FromQuery] string? after = null)
        {
            var episodes = await _service.ListEpisodesAsync(programId: programId, genre: genre, language: language, searchTerm: searchTerm, page: page, size: size, after: after);
            return Ok(episodes);
        }

        /// <summary>
        /// Get episodes for a specific program.
        /// </summary>
        [HttpGet("programs/{programId}/episodes")]
        public async System.Threading.Tasks.Task<IActionResult> GetProgramEpisodes(
            [FromRoute] Guid programId,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            return await ListEpisodes(programId: programId, page: page, size: size);
        }
    }
}
