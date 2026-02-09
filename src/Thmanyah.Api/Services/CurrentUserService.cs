using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Thmanyah.Api.Services
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserName { get; }
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        public string? UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
    }
}
