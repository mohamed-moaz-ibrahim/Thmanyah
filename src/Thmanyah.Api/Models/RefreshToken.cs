using System;
using System.ComponentModel.DataAnnotations;

namespace Thmanyah.Api.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }
        public string UserId { get; set; } = default!;
        public string Token { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public bool Revoked { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
