using System.ComponentModel.DataAnnotations;

namespace Travel_Agency.Models
{
    public enum NotificationType
    {
        PackageAvailable,      // Room became available for waiting user
        BookingConfirmed,      // Booking was successful
        BookingCancelled,      // Booking was cancelled
        WaitingListJoined,     // User joined waiting list
        PriorityExpired,       // User's booking priority expired
        GeneralMessage         // General admin message
    }

    public enum NotificationStatus
    {
        Unread,
        Read,
        ActionTaken,
        Expired
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;

        // For package-related notifications
        public int? TravelPackageId { get; set; }
        public TravelPackage? TravelPackage { get; set; }

        // For waiting list priority booking
        public int? WaitingListId { get; set; }
        
        // Timing
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReadAt { get; set; }
        public DateTime? ExpiresAt { get; set; }  // ⏰ For time-limited priority booking
        
        // Action URL (optional - for quick actions)
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }

        // Helper properties
        public bool IsExpired => ExpiresAt.HasValue && DateTime.Now > ExpiresAt.Value;
        public bool HasAction => !string.IsNullOrEmpty(ActionUrl);
        
        public TimeSpan? TimeRemaining => ExpiresAt.HasValue 
            ? (ExpiresAt.Value > DateTime.Now ? ExpiresAt.Value - DateTime.Now : TimeSpan.Zero) 
            : null;
    }
}
