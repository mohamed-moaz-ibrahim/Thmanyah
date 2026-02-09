using Thmanyah.Shared.Domain;

namespace Thmanyah.Discovery.Services
{
    public interface IDiscoveryService
    {
        System.Threading.Tasks.Task<System.Collections.Generic.List<Program>> ListProgramsAsync(int page = 1, int size = 20, ProgramType? type = null, string? searchTerm = null, string? after = null);

        System.Threading.Tasks.Task<System.Collections.Generic.List<Episode>> ListEpisodesAsync(Guid? programId = null, EpisodeGenre? genre = null, EpisodeLanguage? language = null, string? searchTerm = null, int page = 1, int size = 20, string? after = null);
    }
}
