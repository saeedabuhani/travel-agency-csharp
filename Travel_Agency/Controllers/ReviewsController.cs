//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Travel_Agency.Data;
//using Travel_Agency.Models;
//using Travel_Agency.Services;

//namespace Travel_Agency.Controllers
//{
//    [Authorize]
//    public class ReviewsController : Controller
//    {
//        private readonly TravelAgencyDbContext _context;
//        private readonly UserManager<AppUser> _userManager;

//        public ReviewsController(
//            TravelAgencyDbContext context,
//            UserManager<AppUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        // ================= CREATE REVIEW =================

//        [HttpGet]
//        public async Task<IActionResult> Create(int packageId)
//        {
//            var user = await _userManager.GetUserAsync(User);
//            if (user == null) return Unauthorized();

//            // ✔ בדיקה שהמשתמש שילם על החבילה
//            bool hasPaidBooking = await _context.Bookings.AnyAsync(b =>
//                b.UserId == user.Id &&
//                b.TravelPackageId == packageId &&
//                b.Status == BookingStatus.Paid);

//            if (!hasPaidBooking)
//            {
//                TempData["Error"] = "You can review only packages you have paid for.";
//                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
//            }

//            // ❌ מניעת Review כפול
//            bool alreadyReviewed = await _context.Reviews.AnyAsync(r =>
//                r.UserId == user.Id &&
//                r.TravelPackageId == packageId);

//            if (alreadyReviewed)
//            {
//                TempData["Error"] = "You already reviewed this package.";
//                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
//            }

//            ViewBag.PackageId = packageId;
//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(int packageId, int rating, string? comment)
//        {
//            var user = await _userManager.GetUserAsync(User);
//            if (user == null) return Unauthorized();

//            if (rating < 1 || rating > 5)
//            {
//                ModelState.AddModelError("", "Rating must be between 1 and 5.");
//            }

//            if (!ModelState.IsValid)
//            {
//                ViewBag.PackageId = packageId;
//                return View();
//            }

//            var review = new Review
//            {
//                UserId = user.Id,
//                TravelPackageId = packageId,
//                Rating = rating,
//                Comment = comment,
//                CreatedAt = DateTime.Now
//            };

//            _context.Reviews.Add(review);
//            await _context.SaveChangesAsync();

//            TempData["Success"] = "Thank you for your review!";
//            return RedirectToAction("Details", "TravelPackages", new { id = packageId });
//        }
//    }

//}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travel_Agency.Data;
using Travel_Agency.Models;
using Travel_Agency.Services;

namespace Travel_Agency.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly TravelAgencyDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ReviewsController(
            TravelAgencyDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================= CREATE REVIEW =================

        [HttpGet]
        public async Task<IActionResult> Create(int packageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // ✔ בדיקה שהמשתמש שילם על החבילה
            bool hasPaidBooking = await _context.Bookings.AnyAsync(b =>
                b.UserId == user.Id &&
                b.TravelPackageId == packageId &&
                b.Status == BookingStatus.Paid);

            if (!hasPaidBooking)
            {
                TempData["Error"] = "You can review only packages you have paid for.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            // ❌ מניעת Review כפול
            bool alreadyReviewed = await _context.Reviews.AnyAsync(r =>
                r.UserId == user.Id &&
                r.TravelPackageId == packageId);

            if (alreadyReviewed)
            {
                TempData["Error"] = "You already reviewed this package.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            ViewBag.PackageId = packageId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int packageId, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError("", "Rating must be between 1 and 5.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.PackageId = packageId;
                return View();
            }

            var review = new Review
            {
                UserId = user.Id,
                TravelPackageId = packageId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // 🔔 Notification (Mock) – Review submitted
            NotificationService.Send(
                $"User {user.Id} submitted a review for package #{packageId} (Rating: {rating})"
            );

            TempData["Success"] = "Thank you for your review!";
            return RedirectToAction("Details", "TravelPackages", new { id = packageId });
        }

        // ================= CREATE WEBSITE REVIEW =================
        [HttpGet]
        public IActionResult CreateWebsiteReview()
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return Unauthorized();

            // Check if user has any paid bookings (to ensure they've used the service)
            bool hasPaidBooking = _context.Bookings.Any(b =>
                b.UserId == user.Id &&
                b.Status == BookingStatus.Paid);

            if (!hasPaidBooking)
            {
                TempData["Error"] = "You can review our service only after completing a booking.";
                return RedirectToAction("Index", "Home");
            }

            // Check if user already reviewed
            bool alreadyReviewed = _context.WebsiteReviews.Any(r =>
                r.UserId == user.Id);

            if (alreadyReviewed)
            {
                TempData["Info"] = "You have already submitted a website review. Thank you!";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWebsiteReview(int rating, string? comment, WebsiteReviewType reviewType)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (rating < 1 || rating > 5)
            {
                ModelState.AddModelError("", "Rating must be between 1 and 5.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            // Check if already reviewed
            bool alreadyReviewed = await _context.WebsiteReviews.AnyAsync(r =>
                r.UserId == user.Id);

            if (alreadyReviewed)
            {
                TempData["Error"] = "You have already submitted a website review.";
                return RedirectToAction("Index", "Home");
            }

            var websiteReview = new WebsiteReview
            {
                UserId = user.Id,
                Rating = rating,
                Comment = comment,
                ReviewType = reviewType,
                CreatedAt = DateTime.Now,
                IsPublished = true
            };

            _context.WebsiteReviews.Add(websiteReview);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thank you for your feedback! Your review helps us improve our service.";
            return RedirectToAction("Index", "Home");
        }
    }
}

