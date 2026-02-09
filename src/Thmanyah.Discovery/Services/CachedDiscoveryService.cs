using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Thmanyah.Shared.Configurations;
using Thmanyah.Shared.Domain;

namespace Thmanyah.Discovery.Services
{
    /// <summary>
    /// Simple caching decorator for IDiscoveryService using IDistributedCache (Redis).
    /// Caches responses keyed by query parameters for a short TTL.
    /// </summary>
    public class CachedDiscoveryService : IDiscoveryService
    {
        private readonly IDiscoveryService _inner;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _programsCacheOptions;
        private readonly DistributedCacheEntryOptions _episodesCacheOptions;

        public CachedDiscoveryService(IDiscoveryService inner, IDistributedCache cache, DiscoveryCacheConfiguration? config = null)
        {
            _inner = inner;
            _cache = cache;
            var pTtl = config?.ProgramsTtlSeconds ?? 30;
            var eTtl = config?.EpisodesTtlSeconds ?? 30;

            _programsCacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(pTtl) };
            _episodesCacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(eTtl) };
        }

        private async Task<string> BuildProgramsKeyAsync(int page, int size, ProgramType? type, string? searchTerm, string? after)
        {
            var version = await GetVersionAsync();
            return $"discovery:v{version}:programs:p{page}:s{size}:t{(type?.ToString() ?? "null")}:q{(searchTerm ?? "")} :a{(after ?? "")}".ToLowerInvariant();
        }

        private async Task<string> BuildEpisodesKeyAsync(Guid? programId, EpisodeGenre? genre, EpisodeLanguage? language, string? searchTerm, int page, int size, string? after)
        {
            var version = await GetVersionAsync();
            return $"discovery:v{version}:episodes:pid{(programId?.ToString() ?? "null")}:g{(genre?.ToString() ?? "null")}:l{(language?.ToString() ?? "null")}:q{(searchTerm ?? "")} :p{page}:s{size}:a{(after ?? "")}".ToLowerInvariant();
        }

        private async Task<int> GetVersionAsync()
        {
            var v = await _cache.GetStringAsync("discovery:version");
            if (int.TryParse(v, out var iv)) return iv;
            return 0;
        }

        public async Task<List<Program>> ListProgramsAsync(int page = 1, int size = 20, ProgramType? type = null, string? searchTerm = null, string? after = null)
        {
            var key = await BuildProgramsKeyAsync(page, size, type, searchTerm, after);
            var cached = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<List<Program>>(cached) ?? new List<Program>();
            }

            var result = await _inner.ListProgramsAsync(page, size, type, searchTerm, after);
            var payload = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(key, payload, _programsCacheOptions);
            return result;
        }

        public async Task<List<Episode>> ListEpisodesAsync(Guid? programId = null, EpisodeGenre? genre = null, EpisodeLanguage? language = null, string? searchTerm = null, int page = 1, int size = 20, string? after = null)
        {
            var key = await BuildEpisodesKeyAsync(programId, genre, language, searchTerm, page, size, after);
            var cached = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<List<Episode>>(cached) ?? new List<Episode>();
            }

            var result = await _inner.ListEpisodesAsync(programId, genre, language, searchTerm, page, size, after);
            var payload = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(key, payload, _episodesCacheOptions);
            return result;
        }
    }
}
