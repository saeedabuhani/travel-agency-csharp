using System.ComponentModel.DataAnnotations;

namespace Travel_Agency.Models
{
    public class BookingTimeFrame
    {
        public int Id { get; set; }

        [Required]
        public int TravelPackageId { get; set; }
        public TravelPackage? TravelPackage { get; set; }

        [Display(Name = "Latest Booking Date")]
        [DataType(DataType.Date)]
        public DateTime? LatestBookingDate { get; set; } // Latest date a trip can be booked

        [Display(Name = "Cancellation Allowed Until")]
        [DataType(DataType.Date)]
        public DateTime? CancellationAllowedUntil { get; set; } // Cancellation deadline (relative to trip start date or absolute)

        [Display(Name = "Reminder Days Before Departure")]
        [Range(0, 30)]
        public int? ReminderDaysBeforeDeparture { get; set; } // e.g., 5 days before departure

        [Display(Name = "Cancellation Period (Days Before Trip)")]
        [Range(0, 365)]
        public int? CancellationPeriodDays { get; set; } // Number of days before trip start that cancellation is allowed

        // Helper properties
        public bool CanBookNow => !LatestBookingDate.HasValue || DateTime.Now <= LatestBookingDate.Value;
        
        public bool CanCancel(DateTime tripStartDate)
        {
            if (!CancellationPeriodDays.HasValue && !CancellationAllowedUntil.HasValue)
                return true; // No restrictions

            if (CancellationAllowedUntil.HasValue)
            {
                return DateTime.Now <= CancellationAllowedUntil.Value;
            }

            if (CancellationPeriodDays.HasValue)
            {
                var cancellationDeadline = tripStartDate.AddDays(-CancellationPeriodDays.Value);
                return DateTime.Now <= cancellationDeadline;
            }

            return false;
        }
    }
}
