using Travel_Agency.Models;

namespace Travel_Agency.Models
{
    public enum WaitingListStatus
    {
        Waiting,           // Still waiting in queue
        Notified,          // Has been notified, waiting for response
        Booked,            // User booked the package
        Declined,          // User declined the offer
        Expired,           // User didn't respond in time
        Cancelled          // User cancelled their waiting
    }

    public class WaitingList
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }
        
        public int TravelPackageId { get; set; }
        public TravelPackage? TravelPackage { get; set; }
        
        // Queue Management
        public DateTime JoinedAt { get; set; } = DateTime.Now;
        public int Position { get; set; }  // Position in queue (1 = first)
        
        // Notification & Priority
        public WaitingListStatus Status { get; set; } = WaitingListStatus.Waiting;
        public bool Notified { get; set; } = false;
        public DateTime? NotifiedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }      // ⏰ Time limit to book (24 hours default)
        public DateTime? RespondedAt { get; set; }    // When user responded
        
        // Related notification
        public int? NotificationId { get; set; }

        // Helper properties
        public bool IsExpired => ExpiresAt.HasValue && DateTime.Now > ExpiresAt.Value;
        public bool CanBook => Status == WaitingListStatus.Notified && !IsExpired;
        
        public TimeSpan? TimeRemaining => ExpiresAt.HasValue 
            ? (ExpiresAt.Value > DateTime.Now ? ExpiresAt.Value - DateTime.Now : TimeSpan.Zero) 
            : null;

        public string TimeRemainingDisplay
        {
            get
            {
                if (!TimeRemaining.HasValue) return "";
                var tr = TimeRemaining.Value;
                if (tr.TotalHours >= 1)
                    return $"{(int)tr.TotalHours}h {tr.Minutes}m remaining";
                if (tr.TotalMinutes >= 1)
                    return $"{(int)tr.TotalMinutes}m remaining";
                return "Expiring soon!";
            }
        }
    }
}
