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
    public class NotificationsController : Controller
    {
        private readonly TravelAgencyDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly WaitingListService _waitingListService;

        public NotificationsController(
            TravelAgencyDbContext context,
            UserManager<AppUser> userManager,
            WaitingListService waitingListService)
        {
            _context = context;
            _userManager = userManager;
            _waitingListService = waitingListService;
        }

        // =========================================================
        // 📬 INBOX - User's Notifications
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account");

            // Process any expired waiting list entries first
            var userWaitingPackages = await _context.WaitingLists
                .Where(w => w.UserId == userId && w.Status == WaitingListStatus.Notified)
                .Select(w => w.TravelPackageId)
                .Distinct()
                .ToListAsync();

            foreach (var packageId in userWaitingPackages)
            {
                await _waitingListService.ProcessExpiredNotifications(packageId);
            }

            // Get all notifications for user
            var notifications = await _context.Notifications
                .Include(n => n.TravelPackage)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Get active waiting list entries with priority
            var activeWaiting = await _context.WaitingLists
                .Include(w => w.TravelPackage)
                .Where(w => w.UserId == userId && w.Status == WaitingListStatus.Notified)
                .OrderBy(w => w.ExpiresAt)
                .ToListAsync();

            ViewBag.ActiveWaiting = activeWaiting;
            ViewBag.UnreadCount = notifications.Count(n => n.Status == NotificationStatus.Unread);

            return View(notifications);
        }

        // =========================================================
        // 👁️ MARK AS READ
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification != null && notification.Status == NotificationStatus.Unread)
            {
                notification.Status = NotificationStatus.Read;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ✅ MARK ALL AS READ
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.Status = NotificationStatus.Read;
                notification.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "All notifications marked as read!";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // ❌ DECLINE BOOKING (from waiting list)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineBooking(int waitingListId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account");

            var result = await _waitingListService.DeclineBooking(waitingListId, userId);

            if (result)
            {
                TempData["Info"] = "You have declined this booking opportunity. It has been passed to the next person in the queue.";
            }
            else
            {
                TempData["Error"] = "Could not decline booking. It may have already expired.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 🗑️ DELETE NOTIFICATION
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 🗑️ CLEAR ALL READ NOTIFICATIONS
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAllRead()
        {
            var userId = _userManager.GetUserId(User);
            var readNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.Status == NotificationStatus.Read)
                .ToListAsync();

            _context.Notifications.RemoveRange(readNotifications);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Cleared {readNotifications.Count} read notifications.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 🔔 GET UNREAD COUNT (API for navbar badge)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Json(new { count = 0 });

            var count = await _waitingListService.GetUnreadCount(userId);
            return Json(new { count });
        }
    }
}
