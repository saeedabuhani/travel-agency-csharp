using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travel_Agency.Data;

namespace Travel_Agency.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReportsController : Controller
    {
        private readonly TravelAgencyDbContext _context;

        public AdminReportsController(TravelAgencyDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // ⭐ Top Rated Packages
            ViewBag.TopRated = await _context.Reviews
                .GroupBy(r => r.TravelPackageId)
                .Select(g => new
                {
                    PackageId = g.Key,
                    AvgRating = g.Average(r => r.Rating),
                    ReviewsCount = g.Count()
                })
                .OrderByDescending(x => x.AvgRating)
                .Take(5)
                .ToListAsync();

            // 📦 Most Booked Packages
            ViewBag.MostBooked = await _context.Bookings
                .GroupBy(b => b.TravelPackageId)
                .Select(g => new
                {
                    PackageId = g.Key,
                    BookingsCount = g.Count()
                })
                .OrderByDescending(x => x.BookingsCount)
                .Take(5)
                .ToListAsync();

            // ⏳ Waiting List Stats
            ViewBag.WaitingStats = await _context.WaitingLists
                .GroupBy(w => w.TravelPackageId)
                .Select(g => new
                {
                    PackageId = g.Key,
                    WaitingCount = g.Count()
                })
                .ToListAsync();

            return View();
        }
    }
}
