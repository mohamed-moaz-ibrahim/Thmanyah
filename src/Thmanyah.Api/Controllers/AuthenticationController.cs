using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Thmanyah.Api.Infrastructure;
using Thmanyah.Api.Models;
using Thmanyah.Shared.Configurations;

namespace Thmanyah.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JWTSettings _config;
        private readonly Thmanyah.Api.Services.IRefreshTokenService _refreshService;

        public AuthenticationController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            Thmanyah.Api.Services.IRefreshTokenService refreshService,
            IOptions<JWTSettings> config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _refreshService = refreshService;
            _config = config.Value;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _userManager.FindByEmailAsync(req.Email);
            if (user == null) return Unauthorized();

            if (!await _userManager.CheckPasswordAsync(user, req.Password)) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            var refresh = await _refreshService.CreateRefreshTokenAsync(user.Id);

            return Ok(new TokenResponse { AccessToken = token, RefreshToken = refresh });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
        {
            if (string.IsNullOrEmpty(req.RefreshToken)) return BadRequest();

            var r = await _refreshService.GetRefreshTokenAsync(req.RefreshToken);
            if (r == null || r.Revoked || r.ExpiresAt < DateTime.UtcNow) return Unauthorized();

            var user = await _userManager.FindByIdAsync(r.UserId);
            if (user == null) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            // Optionally rotate refresh token
            await _refreshService.RevokeAsync(r);

            var newRefresh = await _refreshService.CreateRefreshTokenAsync(user.Id);

            return Ok(new TokenResponse { AccessToken = token, RefreshToken = newRefresh });
        }

        private string GenerateJwtToken(IdentityUser user, System.Collections.Generic.IList<string> roles)
        {
            var key = Encoding.UTF8.GetBytes(_config.Key);
            var issuer = _config.Issuer;

            var claims = new List<Claim>
{
    new Claim(JwtRegisteredClaimNames.Sub, user.UserName ?? user.Email ?? user.Id),
    new Claim(ClaimTypes.NameIdentifier, user.Id),
    new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
    new Claim(JwtRegisteredClaimNames.Iss, issuer),
    new Claim(JwtRegisteredClaimNames.Aud, issuer)
}.ToList();

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            claims.AddRange(roles.Select(r => new Claim("role", r)));
            claims.AddRange(roles.Select(r => new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", r)));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config.ExpiryMinutes)),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Refresh token creation and persistence delegated to IRefreshTokenService
    }

    public class LoginRequest
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = default!;
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
    }
}
