namespace Travel_Agency.Models  // ← זה חסר לך!
{
    public class Booking
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int TravelPackageId { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.Now;
        public BookingStatus Status { get; set; } = BookingStatus.Reserved;
        public DateTime? PaidAt { get; set; }
        public bool CreatedFromWaitingList { get; set; }
        public bool IsCancellable => Status == BookingStatus.Reserved;

        public TravelPackage? Package { get; set; }
        public AppUser? User { get; set; }
    }
}