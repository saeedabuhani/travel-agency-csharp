//using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore;
//using Travel_Agency.Models;

//namespace Travel_Agency.Data
//{
//    public class TravelAgencyDbContext : IdentityDbContext<AppUser>
//    {
//        public TravelAgencyDbContext(DbContextOptions<TravelAgencyDbContext> options)
//            : base(options)
//        {
//        }

//        public DbSet<WaitingList> WaitingLists { get; set; }
//        public DbSet<Review> Reviews { get; set; }
//        public DbSet<TravelPackage> TravelPackages { get; set; }
//        public DbSet<Booking> Bookings { get; set; }
//        //public DbSet<ShoppingCart> ShoppingCarts { get; set; }  // ⭐ ADD THIS LINE
//        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            base.OnModelCreating(modelBuilder);

//            modelBuilder.Entity<TravelPackage>()
//                .Property(p => p.Price)
//                .HasPrecision(10, 2);

//            modelBuilder.Entity<TravelPackage>()
//                .Property(p => p.Discount)
//                .HasPrecision(10, 2);
//        }
//    }
//}


using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Travel_Agency.Models;

namespace Travel_Agency.Data
{
    public class TravelAgencyDbContext : IdentityDbContext<AppUser>
    {
        public TravelAgencyDbContext(DbContextOptions<TravelAgencyDbContext> options)
            : base(options)
        {
        }

        public DbSet<TravelPackage> TravelPackages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<WaitingList> WaitingLists { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<Notification> Notifications { get; set; }  // 📬 User notifications/inbox
        public DbSet<BookingTimeFrame> BookingTimeFrames { get; set; }  // ⏰ Booking time frame rules
        public DbSet<WebsiteReview> WebsiteReviews { get; set; }  // ⭐ Website experience reviews

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TravelPackage>()
                .Property(p => p.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<TravelPackage>()
                .Property(p => p.Discount)
                .HasPrecision(10, 2);
        }
    }
}