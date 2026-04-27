

//using ClosedXML.Excel;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;
//using Travel_Agency.Data;
//using Travel_Agency.Models;

//namespace Travel_Agency.Controllers
//{
//    [Authorize(Roles = "Admin")]
//    public class AdminController : Controller
//    {
//        private readonly TravelAgencyDbContext _context;
//        private readonly UserManager<AppUser> _userManager;

//        public AdminController(
//            TravelAgencyDbContext context,
//            UserManager<AppUser> userManager)
//        {
//            _context = context;
//            _userManager = userManager;
//        }

//        // =========================================================
//        // 📊 DASHBOARD
//        // =========================================================
//        public async Task<IActionResult> Dashboard()
//        {
//            ViewBag.TotalPackages = await _context.TravelPackages.CountAsync();
//            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
//            ViewBag.TotalUsers = await _context.Users.CountAsync();

//            ViewBag.TotalRevenue = await _context.Bookings
//                .Include(b => b.Package)
//                .SumAsync(b =>
//                    b.Package.Discount.HasValue && b.Package.Discount > 0
//                        ? b.Package.Discount.Value
//                        : b.Package.Price);

//            ViewBag.BookingsByMonth = await _context.Bookings
//                .GroupBy(b => b.BookingDate.Month)
//                .Select(g => new { Month = g.Key, Count = g.Count() })
//                .OrderBy(g => g.Month)
//                .ToListAsync();

//            ViewBag.BookingsByCountry = await _context.Bookings
//                .Include(b => b.Package)
//                .GroupBy(b => b.Package.Country)
//                .Select(g => new { Country = g.Key, Count = g.Count() })
//                .ToListAsync();

//            ViewBag.RevenueByMonth = await _context.Bookings
//                .Include(b => b.Package)
//                .GroupBy(b => b.BookingDate.Month)
//                .Select(g => new
//                {
//                    Month = g.Key,
//                    Revenue = g.Sum(x =>
//                        x.Package.Discount.HasValue && x.Package.Discount > 0
//                            ? x.Package.Discount.Value
//                            : x.Package.Price)
//                })
//                .OrderBy(g => g.Month)
//                .ToListAsync();

//            ViewBag.TopDestinations = await _context.Bookings
//                .Include(b => b.Package)
//                .GroupBy(b => b.Package.Country)
//                .Select(g => new
//                {
//                    Country = g.Key,
//                    Count = g.Count(),
//                    AvgPrice = g.Average(b => b.Package.Price)
//                })
//                .OrderByDescending(g => g.Count)
//                .Take(5)
//                .ToListAsync();

//            return View();
//        }

//        // =========================================================
//        // 👤 USERS MANAGEMENT
//        // =========================================================
//        public async Task<IActionResult> Users()
//        {
//            var users = await _context.Users.ToListAsync();
//            return View(users);
//        }

//        [HttpPost]
//        public async Task<IActionResult> MakeAdmin(string userId)
//        {
//            var user = await _userManager.FindByIdAsync(userId);
//            if (user == null) return NotFound();

//            if (!await _userManager.IsInRoleAsync(user, "Admin"))
//            {
//                await _userManager.AddToRoleAsync(user, "Admin");
//            }

//            TempData["Success"] = $"User {user.UserName} promoted to Admin.";
//            return RedirectToAction("Users");
//        }

//        // =========================================================
//        // 📄 EXPORT PDF (TABLE)
//        // =========================================================
//        public async Task<IActionResult> ExportPdf()
//        {
//            var bookings = await _context.Bookings
//                .Include(b => b.Package)
//                .ToListAsync();

//            var pdf = Document.Create(container =>
//            {
//                container.Page(page =>
//                {
//                    page.Margin(30);

//                    page.Header().AlignCenter()
//                        .Text("Travel Agency – Monthly Report")
//                        .FontSize(22).Bold();

//                    page.Content().Column(col =>
//                    {
//                        col.Item().Text($"Generated at: {DateTime.Now}");
//                        col.Item().LineHorizontal(1);

//                        col.Item().Table(table =>
//                        {
//                            table.ColumnsDefinition(c =>
//                            {
//                                c.RelativeColumn();
//                                c.RelativeColumn();
//                                c.RelativeColumn();
//                            });

//                            table.Header(h =>
//                            {
//                                h.Cell().Text("Destination");
//                                h.Cell().Text("Country");
//                                h.Cell().Text("Price");
//                            });

//                            foreach (var b in bookings)
//                            {
//                                table.Cell().Text(b.Package.Destination);
//                                table.Cell().Text(b.Package.Country);
//                                table.Cell().Text(b.Package.Price.ToString());
//                            }
//                        });
//                    });
//                });
//            });

//            return File(pdf.GeneratePdf(), "application/pdf", "Report.pdf");
//        }

//        // =========================================================
//        // 📊 EXPORT PDF WITH CHARTS
//        // =========================================================
//        [HttpPost]
//        public IActionResult ExportPdfWithCharts(
//            string chart1, string chart2, string chart3, string chart4)
//        {
//            byte[] Decode(string s) =>
//                Convert.FromBase64String(s.Replace("data:image/png;base64,", ""));

//            var pdf = Document.Create(container =>
//            {
//                container.Page(page =>
//                {
//                    page.Margin(30);

//                    page.Header().AlignCenter()
//                        .Text("Travel Agency – Analytics Report")
//                        .FontSize(22).Bold();

//                    page.Content().Column(col =>
//                    {
//                        col.Item().Image(Decode(chart1));
//                        col.Item().Image(Decode(chart2));
//                        col.Item().Image(Decode(chart3));
//                        col.Item().Image(Decode(chart4));
//                    });
//                });
//            });

//            return File(pdf.GeneratePdf(), "application/pdf", "FullReport.pdf");
//        }

//        // =========================================================
//        // 📊 EXPORT EXCEL
//        // =========================================================
//        public async Task<IActionResult> ExportExcel()
//        {
//            var bookings = await _context.Bookings
//                .Include(b => b.Package)
//                .Include(b => b.User)
//                .ToListAsync();

//            using var wb = new XLWorkbook();
//            var ws = wb.Worksheets.Add("Bookings");

//            ws.Cell(1, 1).Value = "Booking ID";
//            ws.Cell(1, 2).Value = "User";
//            ws.Cell(1, 3).Value = "Destination";
//            ws.Cell(1, 4).Value = "Country";
//            ws.Cell(1, 5).Value = "Price";
//            ws.Cell(1, 6).Value = "Date";

//            int row = 2;
//            foreach (var b in bookings)
//            {
//                ws.Cell(row, 1).Value = b.Id;
//                ws.Cell(row, 2).Value = b.User?.UserName;
//                ws.Cell(row, 3).Value = b.Package?.Destination;
//                ws.Cell(row, 4).Value = b.Package?.Country;
//                ws.Cell(row, 5).Value = b.Package?.Price;
//                ws.Cell(row, 6).Value = b.BookingDate.ToShortDateString();
//                row++;
//            }

//            ws.Columns().AdjustToContents();

//            using var stream = new MemoryStream();
//            wb.SaveAs(stream);

//            return File(stream.ToArray(),
//                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
//                "BookingsReport.xlsx");
//        }
//    }
//}



using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Travel_Agency.Data;
using Travel_Agency.Models;
using Travel_Agency.Services;

namespace Travel_Agency.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly TravelAgencyDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly WaitingListService _waitingListService;  // 📬 NEW

        // 👑 MASTER ADMIN - לא ניתן לשינוי!
        private const string MASTER_ADMIN_EMAIL = "admin@gmail.com";

        public AdminController(
            TravelAgencyDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            WaitingListService waitingListService)  // 📬 NEW
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _waitingListService = waitingListService;  // 📬 NEW
        }

        // =========================================================
        // 📊 DASHBOARD
        // =========================================================
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalPackages = await _context.TravelPackages.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalReviews = await _context.Reviews.CountAsync(); // ⭐ Review count

            var now = DateTime.Now;
            ViewBag.TotalRevenue = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Paid) // ⭐ רק Paid!
                .Include(b => b.Package)
                .SumAsync(b =>
                    b.Package.Discount.HasValue &&
                    b.Package.Discount < b.Package.Price &&
                    b.Package.DiscountStartDate.HasValue &&
                    b.Package.DiscountEndDate.HasValue &&
                    b.Package.DiscountStartDate <= now &&
                    b.Package.DiscountEndDate >= now
                        ? b.Package.Discount.Value
                        : b.Package.Price);

            // ⭐ Packages with 0 rooms and waiting list counts
            ViewBag.PackagesWithWaitingList = await _context.TravelPackages
                .Where(p => p.AvailableRooms == 0)
                .Select(p => new
                {
                    Package = p,
                    WaitingCount = _context.WaitingLists.Count(w => w.TravelPackageId == p.Id && w.Status == WaitingListStatus.Waiting)
                })
                .Where(x => x.WaitingCount > 0)
                .ToListAsync();

            ViewBag.BookingsByMonth = await _context.Bookings
                .GroupBy(b => b.BookingDate.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .OrderBy(g => g.Month)
                .ToListAsync();

            ViewBag.BookingsByCountry = await _context.Bookings
                .Include(b => b.Package)
                .GroupBy(b => b.Package.Country)
                .Select(g => new { Country = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.RevenueByMonth = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Paid)
                .Include(b => b.Package)
                .GroupBy(b => b.BookingDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Revenue = g.Sum(x =>
                        x.Package.Discount.HasValue &&
                        x.Package.Discount < x.Package.Price &&
                        x.Package.DiscountStartDate.HasValue &&
                        x.Package.DiscountEndDate.HasValue &&
                        x.Package.DiscountStartDate <= now &&
                        x.Package.DiscountEndDate >= now
                            ? x.Package.Discount.Value
                            : x.Package.Price)
                })
                .OrderBy(g => g.Month)
                .ToListAsync();

            ViewBag.TopDestinations = await _context.Bookings
                .Include(b => b.Package)
                .GroupBy(b => b.Package.Country)
                .Select(g => new
                {
                    Country = g.Key,
                    Count = g.Count(),
                    AvgPrice = g.Average(b => b.Package.Price)
                })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToListAsync();

            return View();
        }

        // =========================================================
        // 👤 USERS MANAGEMENT
        // =========================================================
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var userViewModels = new List<UserWithRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userViewModels.Add(new UserWithRoleViewModel
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName,
                    DateOfBirth = user.DateOfBirth,
                    IsAdmin = roles.Contains("Admin"),
                    IsMasterAdmin = user.Email == MASTER_ADMIN_EMAIL,
                    IsBlocked = user.IsBlocked,
                    BlockedAt = user.BlockedAt,
                    BlockedReason = user.BlockedReason
                });
            }

            return View(userViewModels);
        }

        // =========================================================
        // ⬆️ MAKE ADMIN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAdmin(string userId)
        {
            // 🔐 רק Master Admin יכול!
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Email != MASTER_ADMIN_EMAIL)
            {
                TempData["Error"] = "❌ Only the master admin can promote users to admin.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // בדיקה אם כבר Admin
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = $"{user.Email} is already an admin.";
                return RedirectToAction(nameof(Users));
            }

            // הפיכה ל-Admin
            var result = await _userManager.AddToRoleAsync(user, "Admin");

            if (result.Succeeded)
            {
                TempData["Success"] = $"✅ {user.Email} has been promoted to Admin!";
            }
            else
            {
                TempData["Error"] = "Failed to promote user to admin.";
            }

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // ⬇️ REMOVE ADMIN (DEMOTE)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdmin(string userId)
        {
            // 🔐 רק Master Admin יכול!
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Email != MASTER_ADMIN_EMAIL)
            {
                TempData["Error"] = "❌ Only the master admin can demote admins.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // 🛡️ הגנה על Master Admin!
            if (user.Email == MASTER_ADMIN_EMAIL)
            {
                TempData["Error"] = "❌ Cannot remove admin role from master admin!";
                return RedirectToAction(nameof(Users));
            }

            // בדיקה אם הוא Admin
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["Error"] = $"{user.Email} is not an admin.";
                return RedirectToAction(nameof(Users));
            }

            // הסרת תפקיד Admin
            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");

            if (result.Succeeded)
            {
                // ודא שהוא User
                if (!await _userManager.IsInRoleAsync(user, "User"))
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                TempData["Success"] = $"✅ {user.Email} has been demoted to User.";
            }
            else
            {
                TempData["Error"] = "Failed to remove admin role.";
            }

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // 🗑️ DELETE USER
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            // 🔐 רק Master Admin יכול!
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Email != MASTER_ADMIN_EMAIL)
            {
                TempData["Error"] = "❌ Only the master admin can delete users.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // 🛡️ הגנה על Master Admin!
            if (user.Email == MASTER_ADMIN_EMAIL)
            {
                TempData["Error"] = "❌ Cannot delete the master admin!";
                return RedirectToAction(nameof(Users));
            }

            // מחיקת הזמנות של המשתמש
            var userBookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .ToListAsync();

            _context.Bookings.RemoveRange(userBookings);

            // מחיקת עגלת קניות
            var userCart = await _context.ShoppingCarts
                .Where(sc => sc.UserId == userId)
                .ToListAsync();

            _context.ShoppingCarts.RemoveRange(userCart);

            // מחיקת waiting list
            var userWaitingList = await _context.WaitingLists
                .Where(wl => wl.UserId == userId)
                .ToListAsync();

            _context.WaitingLists.RemoveRange(userWaitingList);

            // מחיקת reviews
            var userReviews = await _context.Reviews
                .Where(r => r.UserId == userId)
                .ToListAsync();

            _context.Reviews.RemoveRange(userReviews);

            // מחיקת notifications
            var userNotifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            _context.Notifications.RemoveRange(userNotifications);

            await _context.SaveChangesAsync();

            // מחיקת המשתמש
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = $"✅ User {user.Email} has been deleted.";
            }
            else
            {
                TempData["Error"] = "Failed to delete user.";
            }

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // 🚫 BLOCK USER
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string userId, string? reason)
        {
            // 🔐 Only Master Admin can block!
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Email != MASTER_ADMIN_EMAIL)
            {
                TempData["Error"] = "❌ Only the master admin can block users.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // 🛡️ Protect Master Admin!
            if (user.Email == MASTER_ADMIN_EMAIL)
            {
                TempData["Error"] = "❌ Cannot block the master admin!";
                return RedirectToAction(nameof(Users));
            }

            // Block the user
            user.IsBlocked = true;
            user.BlockedAt = DateTime.UtcNow;
            user.BlockedReason = reason ?? "Blocked by administrator";
            user.BlockedByAdminId = currentUser.Id;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = $"🚫 User {user.Email} has been blocked.";
            }
            else
            {
                TempData["Error"] = "Failed to block user.";
            }

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // ✅ UNBLOCK USER
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            // 🔐 Only Master Admin can unblock!
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Email != MASTER_ADMIN_EMAIL)
            {
                TempData["Error"] = "❌ Only the master admin can unblock users.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            // Unblock the user
            user.IsBlocked = false;
            user.BlockedAt = null;
            user.BlockedReason = null;
            user.BlockedByAdminId = null;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = $"✅ User {user.Email} has been unblocked.";
            }
            else
            {
                TempData["Error"] = "Failed to unblock user.";
            }

            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // 📋 WAITING LIST MANAGEMENT
        // =========================================================
        public async Task<IActionResult> WaitingList()
        {
            // Process any expired notifications first
            var packageIds = await _context.WaitingLists
                .Where(w => w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified)
                .Select(w => w.TravelPackageId)
                .Distinct()
                .ToListAsync();

            foreach (var packageId in packageIds)
            {
                await _waitingListService.ProcessExpiredNotifications(packageId);
            }

            // ✅ Only show ACTIVE waiting list entries (Waiting & Notified)
            // Sorted by Position (1st registered = 1st in queue)
            var waitingList = await _context.WaitingLists
                .Include(w => w.User)
                .Include(w => w.TravelPackage)
                .Where(w => w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified)
                .OrderBy(w => w.TravelPackageId)
                .ThenBy(w => w.Position)  // ⭐ First registered = Position 1 = First in list
                .ThenBy(w => w.JoinedAt)  // ⭐ If same position, earlier join time first
                .ToListAsync();

            var packages = await _context.TravelPackages
                .Where(p => _context.WaitingLists.Any(w => w.TravelPackageId == p.Id && 
                           (w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified)))
                .ToListAsync();

            ViewBag.Packages = packages;

            // Stats for display - only active entries
            ViewBag.TotalWaiting = waitingList.Count(w => w.Status == WaitingListStatus.Waiting);
            ViewBag.TotalNotified = waitingList.Count(w => w.Status == WaitingListStatus.Notified);
            ViewBag.TotalActive = waitingList.Count;

            return View(waitingList);
        }

        // =========================================================
        // 📧 NOTIFY WAITING LIST USERS (Priority Queue System)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotifyWaitingUsers(int packageId)
        {
            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null)
            {
                TempData["Error"] = "Package not found.";
                return RedirectToAction(nameof(WaitingList));
            }

            if (package.AvailableRooms <= 0)
            {
                TempData["Error"] = "No rooms available to notify users.";
                return RedirectToAction(nameof(WaitingList));
            }

            // Check if there are users waiting
            var waitingCount = await _context.WaitingLists
                .CountAsync(w => w.TravelPackageId == packageId && w.Status == WaitingListStatus.Waiting);

            if (waitingCount == 0)
            {
                TempData["Error"] = "No users waiting for this package.";
                return RedirectToAction(nameof(WaitingList));
            }

            // Use WaitingListService to notify users with priority queue logic
            int notifiedCount = await _waitingListService.NotifyWaitingUsers(packageId, package.AvailableRooms);

            if (notifiedCount > 0)
            {
                bool isPriority = package.AvailableRooms < waitingCount;
                string message = isPriority
                    ? $"✅ Notified {notifiedCount} user(s) for {package.Destination}! (Priority queue - {WaitingListService.PRIORITY_MINUTES} min to respond)"
                    : $"✅ Notified {notifiedCount} user(s) for {package.Destination}! (Enough rooms for all)";
                
                TempData["Success"] = message;
            }
            else
            {
                TempData["Error"] = "No users were notified.";
            }

            return RedirectToAction(nameof(WaitingList));
        }

        // =========================================================
        // 🗑️ REMOVE FROM WAITING LIST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWaitingList(int waitingListId)
        {
            var entry = await _context.WaitingLists.FindAsync(waitingListId);
            if (entry == null)
            {
                TempData["Error"] = "Entry not found.";
                return RedirectToAction(nameof(WaitingList));
            }

            _context.WaitingLists.Remove(entry);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ User removed from waiting list.";
            return RedirectToAction(nameof(WaitingList));
        }

        // =========================================================
        // ⭐ REVIEWS MANAGEMENT
        // =========================================================
        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.TravelPackage)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }

        // =========================================================
        // 📊 ANALYTICS
        // =========================================================
        public async Task<IActionResult> Analytics()
        {
            var now = DateTime.Now;
            ViewBag.TotalRevenue = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Paid)
                .Include(b => b.Package)
                .SumAsync(b =>
                    b.Package!.Discount.HasValue &&
                    b.Package.Discount < b.Package.Price &&
                    b.Package.DiscountStartDate.HasValue &&
                    b.Package.DiscountEndDate.HasValue &&
                    b.Package.DiscountStartDate <= now &&
                    b.Package.DiscountEndDate >= now
                        ? b.Package.Discount.Value
                        : b.Package.Price);

            ViewBag.PendingBookings = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.Reserved);

            ViewBag.CancelledBookings = await _context.Bookings
                .CountAsync(b => b.Status == BookingStatus.Cancelled);

            ViewBag.TotalUsers = await _userManager.Users.CountAsync();

            ViewBag.AdminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;

            return View();
        }

        // =========================================================
        // 📄 EXPORT PDF (TABLE)
        // =========================================================
        public async Task<IActionResult> ExportPdf()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Package)
                .Include(b => b.User)
                .ToListAsync();

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().AlignCenter()
                        .Text("Travel Agency – Monthly Report")
                        .FontSize(22).Bold();

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Generated at: {DateTime.Now:dd/MM/yyyy HH:mm}");
                        col.Item().LineHorizontal(1);
                        col.Item().PaddingVertical(10);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("User").Bold();
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Destination").Bold();
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Country").Bold();
                                h.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Price").Bold();
                            });

                            foreach (var b in bookings)
                            {
                                table.Cell().Padding(5).Text(b.User?.Email ?? "N/A");
                                table.Cell().Padding(5).Text(b.Package?.Destination ?? "N/A");
                                table.Cell().Padding(5).Text(b.Package?.Country ?? "N/A");
                                table.Cell().Padding(5).Text($"${b.Package?.Price ?? 0}");
                            }
                        });
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf", "Report.pdf");
        }

        // =========================================================
        // 📊 EXPORT PDF WITH CHARTS
        // =========================================================
        [HttpPost]
        public IActionResult ExportPdfWithCharts(
            string chart1, string chart2, string chart3, string chart4)
        {
            byte[] Decode(string s) =>
                Convert.FromBase64String(s.Replace("data:image/png;base64,", ""));

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().AlignCenter()
                        .Text("Travel Agency – Analytics Report")
                        .FontSize(22).Bold();

                    page.Content().Column(col =>
                    {
                        if (!string.IsNullOrEmpty(chart1))
                            col.Item().Image(Decode(chart1));

                        if (!string.IsNullOrEmpty(chart2))
                            col.Item().Image(Decode(chart2));

                        if (!string.IsNullOrEmpty(chart3))
                            col.Item().Image(Decode(chart3));

                        if (!string.IsNullOrEmpty(chart4))
                            col.Item().Image(Decode(chart4));
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf", "FullReport.pdf");
        }

        // =========================================================
        // 📊 EXPORT EXCEL
        // =========================================================
        public async Task<IActionResult> ExportExcel()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Package)
                .Include(b => b.User)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Bookings");

            // Headers
            ws.Cell(1, 1).Value = "Booking ID";
            ws.Cell(1, 2).Value = "User";
            ws.Cell(1, 3).Value = "Destination";
            ws.Cell(1, 4).Value = "Country";
            ws.Cell(1, 5).Value = "Price";
            ws.Cell(1, 6).Value = "Status";
            ws.Cell(1, 7).Value = "Date";

            // Style headers
            ws.Range(1, 1, 1, 7).Style.Font.Bold = true;
            ws.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 2;
            foreach (var b in bookings)
            {
                ws.Cell(row, 1).Value = b.Id;
                ws.Cell(row, 2).Value = b.User?.Email ?? "N/A";
                ws.Cell(row, 3).Value = b.Package?.Destination ?? "N/A";
                ws.Cell(row, 4).Value = b.Package?.Country ?? "N/A";
                ws.Cell(row, 5).Value = b.Package?.Price ?? 0;
                ws.Cell(row, 6).Value = b.Status.ToString();
                ws.Cell(row, 7).Value = b.BookingDate.ToShortDateString();
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BookingsReport.xlsx");
        }

        // =========================================================
        // ⏰ BOOKING TIME FRAMES MANAGEMENT
        // =========================================================
        public async Task<IActionResult> BookingTimeFrames()
        {
            var timeFrames = await _context.BookingTimeFrames
                .Include(tf => tf.TravelPackage)
                .OrderBy(tf => tf.TravelPackage != null ? tf.TravelPackage.Destination : "")
                .ToListAsync();

            var packagesWithoutTimeFrames = await _context.TravelPackages
                .Where(p => !_context.BookingTimeFrames.Any(tf => tf.TravelPackageId == p.Id))
                .ToListAsync();

            ViewBag.PackagesWithoutTimeFrames = packagesWithoutTimeFrames;

            return View(timeFrames);
        }

        [HttpGet]
        public async Task<IActionResult> CreateTimeFrame(int? packageId)
        {
            var packages = await _context.TravelPackages
                .OrderBy(p => p.Destination)
                .ToListAsync();

            ViewBag.Packages = packages;
            ViewBag.SelectedPackageId = packageId;

            if (packageId.HasValue)
            {
                var existing = await _context.BookingTimeFrames
                    .FirstOrDefaultAsync(tf => tf.TravelPackageId == packageId.Value);

                if (existing != null)
                {
                    TempData["Error"] = "This package already has a booking time frame. Please edit it instead.";
                    return RedirectToAction("EditTimeFrame", new { id = existing.Id });
                }
            }

            return View(new BookingTimeFrame { TravelPackageId = packageId ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTimeFrame(BookingTimeFrame timeFrame)
        {
            if (ModelState.IsValid)
            {
                // Check if time frame already exists for this package
                var existing = await _context.BookingTimeFrames
                    .FirstOrDefaultAsync(tf => tf.TravelPackageId == timeFrame.TravelPackageId);

                if (existing != null)
                {
                    ModelState.AddModelError("", "A booking time frame already exists for this package.");
                    var packages = await _context.TravelPackages.OrderBy(p => p.Destination).ToListAsync();
                    ViewBag.Packages = packages;
                    return View(timeFrame);
                }

                _context.BookingTimeFrames.Add(timeFrame);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Booking time frame created successfully!";
                return RedirectToAction(nameof(BookingTimeFrames));
            }

            var allPackages = await _context.TravelPackages.OrderBy(p => p.Destination).ToListAsync();
            ViewBag.Packages = allPackages;
            return View(timeFrame);
        }

        [HttpGet]
        public async Task<IActionResult> EditTimeFrame(int? id)
        {
            if (id == null)
                return NotFound();

            var timeFrame = await _context.BookingTimeFrames
                .Include(tf => tf.TravelPackage)
                .FirstOrDefaultAsync(tf => tf.Id == id);

            if (timeFrame == null)
                return NotFound();

            return View(timeFrame);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTimeFrame(int id, BookingTimeFrame timeFrame)
        {
            if (id != timeFrame.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _context.BookingTimeFrames.FindAsync(id);
                if (existing == null)
                    return NotFound();

                existing.LatestBookingDate = timeFrame.LatestBookingDate;
                existing.CancellationAllowedUntil = timeFrame.CancellationAllowedUntil;
                existing.ReminderDaysBeforeDeparture = timeFrame.ReminderDaysBeforeDeparture;
                existing.CancellationPeriodDays = timeFrame.CancellationPeriodDays;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Booking time frame updated successfully!";
                return RedirectToAction(nameof(BookingTimeFrames));
            }

            return View(timeFrame);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTimeFrame(int id)
        {
            var timeFrame = await _context.BookingTimeFrames.FindAsync(id);
            if (timeFrame != null)
            {
                _context.BookingTimeFrames.Remove(timeFrame);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Booking time frame deleted successfully!";
            }

            return RedirectToAction(nameof(BookingTimeFrames));
        }
    }

    // =========================================================
    // 📋 VIEW MODEL
    // =========================================================
    public class UserWithRoleViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsMasterAdmin { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime? BlockedAt { get; set; }
        public string? BlockedReason { get; set; }

        public int? Age
        {
            get
            {
                if (!DateOfBirth.HasValue)
                    return null;
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;
                if (DateOfBirth.Value.Date > today.AddYears(-age))
                {
                    age--;
                }
                return age;
            }
        }
    }
}