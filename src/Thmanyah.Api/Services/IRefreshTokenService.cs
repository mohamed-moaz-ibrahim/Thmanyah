using System.Threading.Tasks;
using Thmanyah.Api.Models;

namespace Thmanyah.Api.Services
{
    public interface IRefreshTokenService
    {
        Task<string> CreateRefreshTokenAsync(string userId);

        Task<RefreshToken?> GetRefreshTokenAsync(string token);

        Task RevokeAsync(RefreshToken token);
    }
}
