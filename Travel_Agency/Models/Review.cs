//namespace Travel_Agency.Models
//{
//    public class Review
//    {
//        public int Id { get; set; }

//        public int TravelPackageId { get; set; }
//        public TravelPackage TravelPackage { get; set; }

//        public string UserId { get; set; }
//        public AppUser User { get; set; }

//        public int Rating { get; set; } // 1–5 ⭐
//        public string? Comment { get; set; }

//        public DateTime CreatedAt { get; set; } = DateTime.Now;
//    }
//}



// 4. Review.cs
using Travel_Agency.Models;

public class Review
{
    public int Id { get; set; }
    public int TravelPackageId { get; set; }
    public TravelPackage? TravelPackage { get; set; } // ← Add ?
    public string UserId { get; set; } = string.Empty; // ← Add this
    public AppUser? User { get; set; } // ← Add ?
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
