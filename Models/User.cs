using System;

namespace Rebloom.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; } = null!;
        public bool IsVerified { get; set; }
        public string? VerificationToken { get; set; }
        public string? ResetToken { get; set; }
        public string? AuthToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
