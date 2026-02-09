using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Thmanyah.Discovery.Infrastructure;
using Thmanyah.Shared.Domain;

namespace Thmanyah.Discovery.Services
{
    public class DiscoveryService : IDiscoveryService
    {
        private readonly DiscoveryDbContext _db;

        public DiscoveryService(DiscoveryDbContext db)
        {
            _db = db;
        }

        public async Task<List<Program>> ListProgramsAsync(int page = 1, int size = 20, ProgramType? type = null, string? searchTerm = null, string? after = null)
        {
            var query = _db.Programs.AsQueryable();

            if (type.HasValue)
            {
                query = query.Where(p => p.Type == type.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(term) ||
                                         (p.Description != null && p.Description.ToLower().Contains(term)) ||
                                         (p.PresenterName != null && p.PresenterName.ToLower().Contains(term)));
            }

            // Keyset pagination if 'after' cursor is provided (cursor = base64("title|id"))
            if (!string.IsNullOrWhiteSpace(after))
            {
                try
                {
                    var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(after));
                    var parts = decoded.Split('|');
                    if (parts.Length == 2)
                    {
                        var lastTitle = parts[0];
                        var lastId = Guid.Parse(parts[1]);
                        query = query.Where(p => string.Compare(p.Title, lastTitle) > 0 || (p.Title == lastTitle && p.Id.CompareTo(lastId) > 0));
                    }
                }
                catch
                {
                    // ignore malformed cursor and fall back to offset pagination
                }

                return await query
                    .OrderBy(p => p.Title)
                    .Take(size)
                    .ToListAsync();
            }

            var skip = (page - 1) * size;
            return await query
                .OrderBy(p => p.Title)
                .Skip(skip)
                .Take(size)
                .ToListAsync();
        }

        public async Task<List<Episode>> ListEpisodesAsync(Guid? programId = null, EpisodeGenre? genre = null, EpisodeLanguage? language = null, string? searchTerm = null, int page = 1, int size = 20, string? after = null)
        {
            var query = _db.Episodes.AsQueryable();

            if (programId.HasValue && programId.Value != Guid.Empty)
            {
                query = query.Where(e => e.ProgramId == programId.Value);
            }

            if (genre.HasValue)
            {
                query = query.Where(e => e.Genre == genre.Value);
            }

            if (language.HasValue)
            {
                query = query.Where(e => e.Language == language.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(term) ||
                                         (e.Description != null && e.Description.ToLower().Contains(term)));
            }

            // Keyset pagination if 'after' cursor is provided (cursor = base64("ticks|id|title"), ticks = PublishDateUtc.Ticks)
            if (!string.IsNullOrWhiteSpace(after))
            {
                try
                {
                    var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(after));
                    var parts = decoded.Split('|');
                    if (parts.Length >= 2)
                    {
                        var ticks = long.Parse(parts[0]);
                        var lastId = Guid.Parse(parts[1]);
                        var lastTitle = parts.Length >= 3 ? parts[2] : string.Empty;

                        var lastDt = new DateTime(ticks, DateTimeKind.Utc);

                        query = query.Where(e => e.PublishDate < lastDt || (e.PublishDate == lastDt && (string.Compare(e.Title, lastTitle) > 0 || (e.Title == lastTitle && e.Id.CompareTo(lastId) > 0))));
                    }
                }
                catch
                {
                    // ignore malformed cursor and fall back to offset pagination
                }

                return await query
                    .OrderByDescending(e => e.PublishDate)
                    .ThenBy(e => e.Title)
                    .Take(size)
                    .ToListAsync();
            }

            var skip = (page - 1) * size;
            return await query
                .OrderByDescending(e => e.PublishDate)
                .ThenBy(e => e.Title)
                .Skip(skip)
                .Take(size)
                .ToListAsync();
        }
    }
}
