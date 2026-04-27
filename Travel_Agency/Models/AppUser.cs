using Microsoft.AspNetCore.Identity;

namespace Travel_Agency.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }
        
        // 🚫 Block/Ban System
        public bool IsBlocked { get; set; } = false;
        public DateTime? BlockedAt { get; set; }
        public string? BlockedReason { get; set; }
        public string? BlockedByAdminId { get; set; }
    }
}
