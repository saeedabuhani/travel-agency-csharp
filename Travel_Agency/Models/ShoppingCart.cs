//using System.ComponentModel.DataAnnotations.Schema;

//namespace Travel_Agency.Models
//{
//    public class ShoppingCart
//    {
//        public int Id { get; set; }

//        public string UserId { get; set; }
//        public AppUser User { get; set; }

//        public int TravelPackageId { get; set; }
//        public TravelPackage TravelPackage { get; set; }

//        public DateTime AddedAt { get; set; } = DateTime.Now;

//        [NotMapped]
//        public decimal FinalPrice => TravelPackage?.Discount ?? TravelPackage?.Price ?? 0;
//    }
//}

// ===================================================
// FIX NULLABLE WARNINGS - Add to your Models
// ===================================================

//// 1. ShoppingCart.cs
//using System.ComponentModel.DataAnnotations.Schema;
//using Travel_Agency.Models;

//public class ShoppingCart
//{
//    public int Id { get; set; }

//    public string UserId { get; set; } = string.Empty; // ← Add this
//    public AppUser? User { get; set; } // ← Add ?

//    public int TravelPackageId { get; set; }
//    public TravelPackage? TravelPackage { get; set; } // ← Add ?

//    public DateTime AddedAt { get; set; } = DateTime.Now;

//    [NotMapped]
//    public decimal FinalPrice => TravelPackage?.Discount ?? TravelPackage?.Price ?? 0;
//}

using System.ComponentModel.DataAnnotations.Schema;

namespace Travel_Agency.Models
{
    public class ShoppingCart
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }

        public int TravelPackageId { get; set; }
        public TravelPackage? TravelPackage { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public decimal FinalPrice => TravelPackage != null
            ? (TravelPackage.IsDiscountActive ? TravelPackage.Discount ?? TravelPackage.Price : TravelPackage.Price)
            : 0;
    }
}




















