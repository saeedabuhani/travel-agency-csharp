using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Travel_Agency.Data;
using Travel_Agency.Models;

namespace Travel_Agency.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TravelAgencyDbContext _context;

        public HomeController(ILogger<HomeController> logger, TravelAgencyDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var packages = await _context.TravelPackages
                .Where(p => p.IsVisible)
                .ToListAsync();
            
            // Get published website reviews for testimonials section
            var websiteReviews = await _context.WebsiteReviews
                .Include(r => r.User)
                .Where(r => r.IsPublished)
                .OrderByDescending(r => r.CreatedAt)
                .Take(6) // Show latest 6 reviews
                .ToListAsync();

            ViewBag.WebsiteReviews = websiteReviews;
            ViewBag.AverageRating = websiteReviews.Any() 
                ? websiteReviews.Average(r => (double)r.Rating) 
                : 0.0;
            ViewBag.TotalReviews = await _context.WebsiteReviews.CountAsync(r => r.IsPublished);
            ViewBag.TotalPackages = packages.Count;

            return View(packages);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
