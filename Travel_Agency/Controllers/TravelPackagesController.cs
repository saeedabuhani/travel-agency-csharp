
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Travel_Agency.Data;
//using Travel_Agency.Models;
//using Travel_Agency.Services;
//using Travel_Agency.ViewModels;

//namespace Travel_Agency.Controllers
//{
//    [AllowAnonymous]
//    public class TravelPackagesController : Controller
//    {
//        private readonly TravelAgencyDbContext _context;
//        private readonly UserManager<AppUser> _userManager;

//        public TravelPackagesController(
//            TravelAgencyDbContext context,
//            UserManager<AppUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }


//        public async Task<IActionResult> Index(string search, bool onlyDiscounted)
//        {
//            var userId = _userManager.GetUserId(User);

//            var query = _context.TravelPackages.AsQueryable();

//            if (!string.IsNullOrEmpty(search))
//            {
//                query = query.Where(p =>
//                    p.Destination.Contains(search) ||
//                    p.Country.Contains(search));
//            }

//            if (onlyDiscounted)
//            {
//                query = query.Where(p =>
//                    p.Discount.HasValue && p.Discount < p.Price);
//            }

//            var packages = await query
//                .Select(p => new TravelPackageCardViewModel
//                {
//                    Id = p.Id,
//                    Destination = p.Destination,
//                    Country = p.Country,
//                    Description = p.Description,
//                    ImageUrl = p.ImageUrl,
//                    Price = p.Price,
//                    Discount = p.Discount,
//                    AvailableRooms = p.AvailableRooms,

//                    // ✅ חדש
//                    HasUserBooking = userId != null &&
//                        _context.Bookings.Any(b =>
//                            b.TravelPackageId == p.Id &&
//                            b.UserId == userId &&
//                            b.Status != BookingStatus.Cancelled),

//                    IsUserInWaitingList = userId != null &&
//                        _context.WaitingLists.Any(w =>
//                            w.TravelPackageId == p.Id &&
//                            w.UserId == userId)
//                })
//                .ToListAsync();

//            return View(packages);
//        }


//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null) return NotFound();

//            var package = await _context.TravelPackages
//                .FirstOrDefaultAsync(p => p.Id == id);

//            if (package == null) return NotFound();

//            Booking? userBooking = null;
//            WaitingList? userWaitingEntry = null;
//            bool isUserFirstInQueue = false;

//            if (User.Identity!.IsAuthenticated)
//            {
//                var userId = _userManager.GetUserId(User);

//                // 🔍 בדוק אם יש הזמנה קיימת
//                userBooking = await _context.Bookings
//                    .FirstOrDefaultAsync(b =>
//                        b.TravelPackageId == id &&
//                        b.UserId == userId &&
//                        b.Status != BookingStatus.Cancelled);

//                // 🔍 בדוק אם המשתמש בתור
//                userWaitingEntry = await _context.WaitingLists
//                    .FirstOrDefaultAsync(w =>
//                        w.TravelPackageId == id &&
//                        w.UserId == userId);

//                // 🔍 האם הוא הראשון בתור?
//                if (userWaitingEntry != null)
//                {
//                    var firstInQueue = await _context.WaitingLists
//                        .Where(w => w.TravelPackageId == id)
//                        .OrderBy(w => w.JoinedAt)
//                        .FirstOrDefaultAsync();

//                    isUserFirstInQueue = (firstInQueue?.UserId == userId);
//                }
//            }

//            // 📊 סטטיסטיקות
//            ViewBag.WaitingCount = await _context.WaitingLists
//                .CountAsync(w => w.TravelPackageId == id);

//            ViewBag.Reviews = await _context.Reviews
//                .Where(r => r.TravelPackageId == id)
//                .OrderByDescending(r => r.CreatedAt)
//                .ToListAsync();

//            ViewBag.AvgRating = await _context.Reviews
//                .Where(r => r.TravelPackageId == id)
//                .AverageAsync(r => (double?)r.Rating) ?? 0;

//            ViewBag.UserBooking = userBooking;
//            ViewBag.UserWaitingEntry = userWaitingEntry;
//            ViewBag.IsUserFirstInQueue = isUserFirstInQueue; // ⭐ חדש

//            return View(package);
//        }

//        // ===================== CREATE =====================
//        [Authorize(Roles = "Admin")]
//        public IActionResult Create()
//        {
//            return View();
//        }

//        [Authorize(Roles = "Admin")]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(TravelPackage travelPackage)
//        {
//            ValidateTravelPackage(travelPackage);

//            if (ModelState.IsValid)
//            {
//                _context.TravelPackages.Add(travelPackage);
//                await _context.SaveChangesAsync();

//                TempData["Success"] = "Travel package created successfully!";
//                return RedirectToAction(nameof(Index));
//            }

//            return View(travelPackage);
//        }

//        [Authorize(Roles = "Admin")]
//        [HttpGet]
//        public async Task<IActionResult> Edit(int? id)
//        {
//            if (id == null)
//                return NotFound();

//            var travelPackage = await _context.TravelPackages.FindAsync(id);
//            if (travelPackage == null)
//                return NotFound();

//            // ⭐ הוסף את זה
//            ViewBag.WaitingCount = await _context.WaitingLists
//                .CountAsync(w => w.TravelPackageId == id);

//            return View(travelPackage);
//        }


//        [Authorize(Roles = "Admin")]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, TravelPackage model)
//        {
//            if (id != model.Id)
//                return NotFound();

//            ValidateTravelPackage(model);

//            if (!ModelState.IsValid)
//                return View(model);

//            var existingPackage = await _context.TravelPackages.FindAsync(id);
//            if (existingPackage == null)
//                return NotFound();

//            existingPackage.Destination = model.Destination;
//            existingPackage.Country = model.Country;
//            existingPackage.Price = model.Price;
//            existingPackage.Discount = model.Discount;
//            existingPackage.Nights = model.Nights;
//            existingPackage.AvailableRooms = model.AvailableRooms;
//            existingPackage.PackageType = model.PackageType;
//            existingPackage.AgeLimit = model.AgeLimit;
//            existingPackage.Description = model.Description;
//            existingPackage.ImageUrl = model.ImageUrl;
//            existingPackage.StartDate = model.StartDate;
//            existingPackage.EndDate = model.EndDate;
//            existingPackage.DiscountStartDate = model.DiscountStartDate;
//            existingPackage.DiscountEndDate = model.DiscountEndDate;

//            await _context.SaveChangesAsync();

//            TempData["Success"] = "✅ Travel package updated successfully!";
//            return RedirectToAction(nameof(Index));
//        }



//        [Authorize(Roles = "Admin")]
//        [HttpPost]
//        public async Task<IActionResult> AddRooms(int packageId, int roomsToAdd)
//        {
//            if (roomsToAdd <= 0)
//            {
//                TempData["Error"] = "Number of rooms must be positive.";
//                return RedirectToAction("Edit", new { id = packageId });
//            }

//            var package = await _context.TravelPackages.FindAsync(packageId);
//            if (package == null)
//            {
//                TempData["Error"] = "Package not found.";
//                return RedirectToAction("Index");
//            }

//            // ✅ הוסף חדרים
//            package.AvailableRooms += roomsToAdd;
//            await _context.SaveChangesAsync();

//            // 🔔 שלח הודעות לממתינים (FIFO)
//            await NotifyWaitingListUsers(packageId, roomsToAdd);

//            TempData["Success"] = $"✅ Added {roomsToAdd} rooms. Waiting list users have been notified.";
//            return RedirectToAction("Edit", new { id = packageId });
//        }

//        /// <summary>
//        /// שולח הודעות למספר הממתינים הראשונים (FIFO)
//        /// </summary>
//        private async Task NotifyWaitingListUsers(int packageId, int availableRooms)
//        {
//            // 🔍 מצא את הממתינים הראשונים לפי תור (FIFO)
//            var waitingUsers = await _context.WaitingLists
//                .Include(w => w.User)
//                .Include(w => w.TravelPackage)
//                .Where(w => w.TravelPackageId == packageId && !w.Notified)
//                .OrderBy(w => w.JoinedAt) // ⭐ FIFO - הראשון שהצטרף, הראשון שמקבל
//                .Take(availableRooms) // ⭐ רק כמה שיש חדרים!
//                .ToListAsync();

//            if (!waitingUsers.Any())
//                return;

//            foreach (var waiting in waitingUsers)
//            {
//                // 📧 שלח אימייל
//                await NotificationService.SendAsync(
//                    waiting.User.Email,
//                    "🎉 Room Available - It's Your Turn!",
//                    $@"
//                <h2>Good news, {waiting.User.UserName}!</h2>
//                <p>A room is now available for <strong>{waiting.TravelPackage.Destination}</strong>.</p>
//                <p><strong>It's your turn to book!</strong></p>
//                <p>You have <strong>24 hours</strong> to complete your booking before we offer it to the next person.</p>
//                <p><a href='https://yoursite.com/TravelPackages/Details/{waiting.TravelPackageId}'>Book Now</a></p>
//            "
//                );

//                // ✅ סמן שנשלחה הודעה
//                waiting.Notified = true;
//                waiting.NotifiedAt = DateTime.Now;
//            }

//            await _context.SaveChangesAsync();

//            Console.WriteLine($"[WAITING LIST] Notified {waitingUsers.Count} users for package {packageId}");
//        }
//        // ===================== DELETE =====================
//        [Authorize(Roles = "Admin")]
//        public async Task<IActionResult> Delete(int? id)
//        {
//            if (id == null) return NotFound();

//            var travelPackage = await _context.TravelPackages
//                .FirstOrDefaultAsync(m => m.Id == id);

//            if (travelPackage == null) return NotFound();

//            return View(travelPackage);
//        }

//        [Authorize(Roles = "Admin")]
//        [HttpPost, ActionName("Delete")]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var travelPackage = await _context.TravelPackages.FindAsync(id);
//            if (travelPackage != null)
//            {
//                _context.TravelPackages.Remove(travelPackage);
//                await _context.SaveChangesAsync();
//            }

//            TempData["Success"] = "Travel package deleted successfully!";
//            return RedirectToAction(nameof(Index));
//        }

//        private void ValidateTravelPackage(TravelPackage travelPackage)
//        {
//            if (travelPackage.Price <= 0)
//                ModelState.AddModelError("Price", "Price must be greater than 0");

//            if (travelPackage.Discount.HasValue)
//            {
//                if (travelPackage.Discount <= 0)
//                    ModelState.AddModelError("Discount", "Discount must be positive");

//                if (travelPackage.Discount >= travelPackage.Price)
//                    ModelState.AddModelError("Discount", "Discount must be lower than price");
//            }

//            if (travelPackage.EndDate <= travelPackage.StartDate)
//            {
//                ModelState.AddModelError("", "End date must be after start date");
//            }

//            if (travelPackage.DiscountStartDate.HasValue &&
//                travelPackage.DiscountEndDate.HasValue)
//            {
//                if (travelPackage.DiscountEndDate <= travelPackage.DiscountStartDate)
//                {
//                    ModelState.AddModelError(
//                        "DiscountEndDate",
//                        "Discount end date must be after start date");
//                }
//                else if ((travelPackage.DiscountEndDate.Value -
//                          travelPackage.DiscountStartDate.Value).TotalDays > 7)
//                {
//                    ModelState.AddModelError(
//                        "DiscountEndDate",
//                        "Discount period cannot exceed 7 days");
//                }
//            }

//            // ✅⬇️⬇️⬇️ הוספה כאן ⬇️⬇️⬇️
//            if (travelPackage.Nights <= 0)
//            {
//                ModelState.AddModelError(
//                    "Nights",
//                    "Nights must be greater than 0");
//            }
//        }

//    }
//}


// ===================================================
// Controllers/TravelPackagesController.cs
// UPDATED VERSION with Sorting & Filtering
// ===================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travel_Agency.Data;
using Travel_Agency.Models;
using Travel_Agency.Services;
using Travel_Agency.ViewModels;

namespace Travel_Agency.Controllers
{
    [AllowAnonymous]
    public class TravelPackagesController : Controller
    {
        private readonly TravelAgencyDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly WaitingListService _waitingListService;

        public TravelPackagesController(
            TravelAgencyDbContext context,
            UserManager<AppUser> userManager,
            WaitingListService waitingListService)
        {
            _context = context;
            _userManager = userManager;
            _waitingListService = waitingListService;
        }

        // ============ INDEX with SORTING & FILTERING ============
        public async Task<IActionResult> Index(
            string search,
            bool onlyDiscounted,
            string sortBy,
            string country,
            string packageType,
            decimal? minPrice,
            decimal? maxPrice,
            DateTime? startDateFrom,
            DateTime? startDateTo)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var now = DateTime.Now;

            var query = _context.TravelPackages.AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(p => p.IsVisible);
            }

            // ✅ SEARCH
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.Destination.Contains(search) ||
                    p.Country.Contains(search) ||
                    (p.Title != null && p.Title.Contains(search)) ||
                    p.Description.Contains(search) ||
                    p.PackageType.Contains(search));
            }

            // ✅ FILTER: Discounted Only
            if (onlyDiscounted)
            {
                query = query.Where(p =>
                    p.Discount.HasValue &&
                    p.Discount < p.Price &&
                    p.DiscountStartDate.HasValue &&
                    p.DiscountEndDate.HasValue &&
                    p.DiscountStartDate <= now &&
                    p.DiscountEndDate >= now);
            }

            // ✅ FILTER: Country
            if (!string.IsNullOrEmpty(country))
            {
                query = query.Where(p => p.Country == country);
            }

            // ✅ FILTER: Package Type
            if (!string.IsNullOrEmpty(packageType))
            {
                query = query.Where(p => p.PackageType == packageType);
            }

            // ✅ FILTER: Price Range
            if (minPrice.HasValue)
            {
                query = query.Where(p =>
                    (p.Discount.HasValue &&
                     p.Discount < p.Price &&
                     p.DiscountStartDate.HasValue &&
                     p.DiscountEndDate.HasValue &&
                     p.DiscountStartDate <= now &&
                     p.DiscountEndDate >= now &&
                     p.Discount.Value >= minPrice.Value) ||
                    (!(p.Discount.HasValue &&
                       p.Discount < p.Price &&
                       p.DiscountStartDate.HasValue &&
                       p.DiscountEndDate.HasValue &&
                       p.DiscountStartDate <= now &&
                       p.DiscountEndDate >= now) &&
                     p.Price >= minPrice.Value));
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p =>
                    (p.Discount.HasValue &&
                     p.Discount < p.Price &&
                     p.DiscountStartDate.HasValue &&
                     p.DiscountEndDate.HasValue &&
                     p.DiscountStartDate <= now &&
                     p.DiscountEndDate >= now &&
                     p.Discount.Value <= maxPrice.Value) ||
                    (!(p.Discount.HasValue &&
                       p.Discount < p.Price &&
                       p.DiscountStartDate.HasValue &&
                       p.DiscountEndDate.HasValue &&
                       p.DiscountStartDate <= now &&
                       p.DiscountEndDate >= now) &&
                     p.Price <= maxPrice.Value));
            }

            // ✅ FILTER: Travel Date Range
            if (startDateFrom.HasValue)
            {
                query = query.Where(p => p.StartDate >= startDateFrom.Value);
            }

            if (startDateTo.HasValue)
            {
                query = query.Where(p => p.StartDate <= startDateTo.Value);
            }

            // ✅ SORTING
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p =>
                    (p.Discount.HasValue &&
                     p.Discount < p.Price &&
                     p.DiscountStartDate.HasValue &&
                     p.DiscountEndDate.HasValue &&
                     p.DiscountStartDate <= now &&
                     p.DiscountEndDate >= now)
                        ? p.Discount.Value
                        : p.Price),
                "price_desc" => query.OrderByDescending(p =>
                    (p.Discount.HasValue &&
                     p.Discount < p.Price &&
                     p.DiscountStartDate.HasValue &&
                     p.DiscountEndDate.HasValue &&
                     p.DiscountStartDate <= now &&
                     p.DiscountEndDate >= now)
                        ? p.Discount.Value
                        : p.Price),
                "name_asc" => query.OrderBy(p => p.Destination),
                "name_desc" => query.OrderByDescending(p => p.Destination),
                "date_asc" => query.OrderBy(p => p.StartDate),
                "date_desc" => query.OrderByDescending(p => p.StartDate),
                "category" => query.OrderBy(p => p.PackageType),
                "popular" => query.OrderByDescending(p =>
                    _context.Bookings.Count(b => b.TravelPackageId == p.Id && b.Status == BookingStatus.Paid)),
                _ => query.OrderBy(p => p.Id)
            };

            var packages = await query
                .Select(p => new TravelPackageCardViewModel
                {
                    Id = p.Id,
                    Destination = p.Destination,
                    Country = p.Country,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    Discount = p.Discount,
                    IsDiscountActive = p.Discount.HasValue &&
                                       p.Discount < p.Price &&
                                       p.DiscountStartDate.HasValue &&
                                       p.DiscountEndDate.HasValue &&
                                       p.DiscountStartDate <= now &&
                                       p.DiscountEndDate >= now,
                    FinalPrice = (p.Discount.HasValue &&
                                  p.Discount < p.Price &&
                                  p.DiscountStartDate.HasValue &&
                                  p.DiscountEndDate.HasValue &&
                                  p.DiscountStartDate <= now &&
                                  p.DiscountEndDate >= now)
                        ? p.Discount.Value
                        : p.Price,
                    AvailableRooms = p.AvailableRooms,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    PackageType = p.PackageType,
                    AgeLimit = p.AgeLimit,
                    IsVisible = p.IsVisible,

                    HasUserBooking = userId != null &&
                        _context.Bookings.Any(b =>
                            b.TravelPackageId == p.Id &&
                            b.UserId == userId &&
                            b.Status != BookingStatus.Cancelled),

                    IsUserInWaitingList = userId != null &&
                        _context.WaitingLists.Any(w =>
                            w.TravelPackageId == p.Id &&
                            w.UserId == userId)
                })
                .ToListAsync();

            // ✅ ViewBag for filters
            ViewBag.Countries = await _context.TravelPackages
                .Where(p => isAdmin || p.IsVisible)
                .Select(p => p.Country)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.PackageTypes = await _context.TravelPackages
                .Where(p => isAdmin || p.IsVisible)
                .Select(p => p.PackageType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            ViewBag.TotalCount = packages.Count;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentSort = sortBy;
            ViewBag.CurrentCountry = country;
            ViewBag.CurrentPackageType = packageType;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentStartDateFrom = startDateFrom?.ToString("yyyy-MM-dd");
            ViewBag.CurrentStartDateTo = startDateTo?.ToString("yyyy-MM-dd");
            ViewBag.OnlyDiscounted = onlyDiscounted;

            return View(packages);
        }

        // ============ DETAILS ============
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var package = await _context.TravelPackages
                .FirstOrDefaultAsync(p => p.Id == id);

            if (package == null) return NotFound();

            if (!User.IsInRole("Admin") && !package.IsVisible)
            {
                return NotFound();
            }

            Booking? userBooking = null;
            WaitingList? userWaitingEntry = null;
            bool isUserFirstInQueue = false;

            if (User.Identity!.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);

                userBooking = await _context.Bookings
                    .FirstOrDefaultAsync(b =>
                        b.TravelPackageId == id &&
                        b.UserId == userId &&
                        b.Status != BookingStatus.Cancelled);

                // ✅ Only get ACTIVE waiting list entries (Waiting or Notified)
                // Users who already booked or declined can join the list again
                userWaitingEntry = await _context.WaitingLists
                    .FirstOrDefaultAsync(w =>
                        w.TravelPackageId == id &&
                        w.UserId == userId &&
                        (w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified));

                if (userWaitingEntry != null)
                {
                    var firstInQueue = await _context.WaitingLists
                        .Where(w => w.TravelPackageId == id &&
                                   (w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified))
                        .OrderBy(w => w.Position)
                        .ThenBy(w => w.JoinedAt)
                        .FirstOrDefaultAsync();

                    isUserFirstInQueue = (firstInQueue?.UserId == userId);
                }
            }

            // ✅ Only count ACTIVE waiting list entries
            var waitingCount = await _context.WaitingLists
                .CountAsync(w => w.TravelPackageId == id &&
                               (w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified));

            ViewBag.WaitingCount = waitingCount;
            var estimatePosition = userWaitingEntry?.Position ?? (waitingCount + 1);
            ViewBag.EstimatedAvailability = await _waitingListService.EstimateAvailability(package.Id, estimatePosition);

            ViewBag.Reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.TravelPackageId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.AvgRating = await _context.Reviews
                .Where(r => r.TravelPackageId == id)
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            ViewBag.UserBooking = userBooking;
            ViewBag.UserWaitingEntry = userWaitingEntry;
            ViewBag.IsUserFirstInQueue = isUserFirstInQueue;

            return View(package);
        }

        // ============ CREATE ============
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new TravelPackage { IsVisible = true });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TravelPackage travelPackage)
        {
            ValidateTravelPackage(travelPackage);

            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(travelPackage.Title))
                {
                    travelPackage.Title = travelPackage.Destination;
                }

                _context.TravelPackages.Add(travelPackage);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Travel package created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(travelPackage);
        }

        // ============ EDIT ============
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var travelPackage = await _context.TravelPackages.FindAsync(id);
            if (travelPackage == null)
                return NotFound();

            ViewBag.WaitingCount = await _context.WaitingLists
                .CountAsync(w => w.TravelPackageId == id);

            return View(travelPackage);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TravelPackage model)
        {
            if (id != model.Id)
                return NotFound();

            ValidateTravelPackage(model);

            if (!ModelState.IsValid)
                return View(model);

            var existingPackage = await _context.TravelPackages.FindAsync(id);
            if (existingPackage == null)
                return NotFound();

            // ⭐ Track if rooms were added
            int previousRooms = existingPackage.AvailableRooms;
            int newRooms = model.AvailableRooms;
            int roomsAdded = newRooms - previousRooms;

            existingPackage.Destination = model.Destination;
            existingPackage.Country = model.Country;
            existingPackage.Price = model.Price;
            existingPackage.Discount = model.Discount;
            existingPackage.Nights = model.Nights;
            existingPackage.AvailableRooms = model.AvailableRooms;
            existingPackage.PackageType = model.PackageType;
            existingPackage.AgeLimit = model.AgeLimit;
            existingPackage.Description = model.Description;
            existingPackage.ImageUrl = model.ImageUrl;
            existingPackage.StartDate = model.StartDate;
            existingPackage.EndDate = model.EndDate;
            existingPackage.DiscountStartDate = model.DiscountStartDate;
            existingPackage.DiscountEndDate = model.DiscountEndDate;
            existingPackage.IsVisible = model.IsVisible;
            existingPackage.Title = string.IsNullOrWhiteSpace(model.Title)
                ? model.Destination
                : model.Title;

            await _context.SaveChangesAsync();

            // ⭐ AUTO-NOTIFY waiting users if rooms were added
            if (roomsAdded > 0)
            {
                await NotifyWaitingListUsers(id, newRooms);
                TempData["Success"] = $"✅ Package updated! {roomsAdded} rooms added - waiting list users notified.";
            }
            else
            {
                TempData["Success"] = "✅ Travel package updated successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // ============ ADD ROOMS ============
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddRooms(int packageId, int roomsToAdd)
        {
            if (roomsToAdd <= 0)
            {
                TempData["Error"] = "Number of rooms must be positive.";
                return RedirectToAction("Edit", new { id = packageId });
            }

            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null)
            {
                TempData["Error"] = "Package not found.";
                return RedirectToAction("Index");
            }

            package.AvailableRooms += roomsToAdd;
            await _context.SaveChangesAsync();

            await NotifyWaitingListUsers(packageId, roomsToAdd);

            TempData["Success"] = $"✅ Added {roomsToAdd} rooms. Waiting list users notified.";
            return RedirectToAction("Edit", new { id = packageId });
        }

        private async Task NotifyWaitingListUsers(int packageId, int availableRooms)
        {
            var waitingUsers = await _context.WaitingLists
                .Include(w => w.User)
                .Include(w => w.TravelPackage)
                .Where(w => w.TravelPackageId == packageId && w.Status == WaitingListStatus.Waiting)
                .OrderBy(w => w.Position)
                .ThenBy(w => w.JoinedAt)
                .Take(availableRooms)
                .ToListAsync();

            if (!waitingUsers.Any())
                return;

            foreach (var waiting in waitingUsers)
            {
                // ✅ Send email notification
                await NotificationService.SendAsync(
                    waiting.User.Email,
                    "🎉 Room Available - It's Your Turn!",
                    $@"
                <h2>Good news, {waiting.User.UserName}!</h2>
                <p>A room is now available for <strong>{waiting.TravelPackage.Destination}</strong>.</p>
                <p><strong>It's your turn to book!</strong></p>
                <p>You have <strong>10 MINUTES</strong> to complete your booking before the next person in queue gets priority.</p>
                <p><a href='/Bookings/MyBookings'>Book Now!</a></p>
            "
                );

                // ✅ Update waiting list entry status
                waiting.Notified = true;
                waiting.NotifiedAt = DateTime.Now;
                waiting.Status = WaitingListStatus.Notified;  // ⭐ CRITICAL: Set status!
                waiting.ExpiresAt = DateTime.Now.AddMinutes(10);  // ⭐ 10-minute priority window

                // ✅ Create in-app notification
                var notification = new Notification
                {
                    UserId = waiting.UserId,
                    Title = $"🎉 Your turn! {waiting.TravelPackage.Destination} is available!",
                    Message = $"A room is now available for <strong>{waiting.TravelPackage.Destination}</strong>! You have <strong>10 MINUTES</strong> to book! ⏰",
                    Type = NotificationType.PackageAvailable,
                    TravelPackageId = packageId,
                    WaitingListId = waiting.Id,
                    ExpiresAt = waiting.ExpiresAt,
                    ActionUrl = "/Bookings/MyBookings",
                    ActionText = "Book Now"
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        // ============ DELETE ============
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var travelPackage = await _context.TravelPackages
                .FirstOrDefaultAsync(m => m.Id == id);

            if (travelPackage == null) return NotFound();

            return View(travelPackage);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var travelPackage = await _context.TravelPackages.FindAsync(id);
            if (travelPackage != null)
            {
                _context.TravelPackages.Remove(travelPackage);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Travel package deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ============ VALIDATION ============
        private void ValidateTravelPackage(TravelPackage travelPackage)
        {
            if (travelPackage.Price <= 0)
                ModelState.AddModelError("Price", "Price must be greater than 0");

            if (travelPackage.Discount.HasValue)
            {
                if (travelPackage.Discount <= 0)
                    ModelState.AddModelError("Discount", "Discount must be positive");

                if (travelPackage.Discount >= travelPackage.Price)
                    ModelState.AddModelError("Discount", "Discount must be lower than price");

                if (!travelPackage.DiscountStartDate.HasValue || !travelPackage.DiscountEndDate.HasValue)
                {
                    ModelState.AddModelError("DiscountStartDate", "Discount start and end dates are required when a discount is set");
                }
            }

            if (travelPackage.EndDate <= travelPackage.StartDate)
            {
                ModelState.AddModelError("", "End date must be after start date");
            }

            if (travelPackage.DiscountStartDate.HasValue &&
                travelPackage.DiscountEndDate.HasValue)
            {
                if (travelPackage.DiscountEndDate <= travelPackage.DiscountStartDate)
                {
                    ModelState.AddModelError(
                        "DiscountEndDate",
                        "Discount end date must be after start date");
                }
                else if ((travelPackage.DiscountEndDate.Value -
                          travelPackage.DiscountStartDate.Value).TotalDays > 7)
                {
                    ModelState.AddModelError(
                        "DiscountEndDate",
                        "Discount period cannot exceed 7 days");
                }
                else if (travelPackage.DiscountEndDate.Value > travelPackage.StartDate)
                {
                    ModelState.AddModelError(
                        "DiscountEndDate",
                        "Discount period must end before the travel start date. Discounts are for early bookings only.");
                }
            }

            if (travelPackage.Nights <= 0)
            {
                ModelState.AddModelError(
                    "Nights",
                    "Nights must be greater than 0");
            }
        }
    }
}