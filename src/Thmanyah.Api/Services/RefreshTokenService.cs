using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Thmanyah.Api.Models;
using Thmanyah.Api.Infrastructure;

namespace Thmanyah.Api.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AuthDbContext _db;

        public RefreshTokenService(AuthDbContext db)
        {
            _db = db;
        }

        public async Task<string> CreateRefreshTokenAsync(string userId)
        {
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Guid.NewGuid().ToString("N");
            var refresh = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Revoked = false
            };

            _db.RefreshTokens ??= _db.Set<RefreshToken>();
            await _db.RefreshTokens.AddAsync(refresh);
            await _db.SaveChangesAsync();

            return token;
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            _db.RefreshTokens ??= _db.Set<RefreshToken>();
            return await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
        }

        public async Task RevokeAsync(RefreshToken token)
        {
            token.Revoked = true;
            await _db.SaveChangesAsync();
        }
    }
}
