namespace Travel_Agency.ViewModels  // ← זה חסר לך!
{
    public class TravelPackageCardViewModel
    {
        public int Id { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public bool IsDiscountActive { get; set; }
        public decimal FinalPrice { get; set; }
        public int AvailableRooms { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string PackageType { get; set; } = string.Empty;
        public int AgeLimit { get; set; }
        public bool IsVisible { get; set; }
        public bool HasUserBooking { get; set; }
        public bool IsUserInWaitingList { get; set; }
        public int? WaitingListPosition { get; set; }
    }
}