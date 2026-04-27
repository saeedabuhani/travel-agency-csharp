using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travel_Agency.Data;
using Travel_Agency.Models;

namespace Travel_Agency.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly TravelAgencyDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ShoppingCartController(
            TravelAgencyDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============ SHOPPING CART INDEX ============
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // 🔄 Auto-expire waiting list entries that have passed 10 minutes
            await ExpireWaitingListEntries();

            var cartItems = await _context.ShoppingCarts
                .Include(sc => sc.TravelPackage)
                .Where(sc => sc.UserId == user.Id)
                .ToListAsync();

            // ⏳ Get user's active waiting list entries
            var waitingListItems = await _context.WaitingLists
                .Include(w => w.TravelPackage)
                .Where(w => w.UserId == user.Id && 
                      (w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified))
                .OrderBy(w => w.JoinedAt)
                .ToListAsync();

            // ⭐ תמיד מגדיר Total, גם אם ריק
            ViewBag.Total = cartItems.Any() ? cartItems.Sum(item => item.FinalPrice) : 0m;
            ViewBag.WaitingListItems = waitingListItems;

            return View(cartItems);
        }

        // 🔄 Auto-expire waiting list entries and notify next user
        private async Task ExpireWaitingListEntries()
        {
            var expiredEntries = await _context.WaitingLists
                .Include(w => w.TravelPackage)
                .Include(w => w.User)
                .Where(w => w.Status == WaitingListStatus.Notified 
                         && w.ExpiresAt != null 
                         && w.ExpiresAt < DateTime.Now)
                .ToListAsync();

            foreach (var expired in expiredEntries)
            {
                expired.Status = WaitingListStatus.Expired;
                
                // Create notification about expiration
                var expiredNotification = new Notification
                {
                    UserId = expired.UserId,
                    Title = "⏰ Booking opportunity expired",
                    Message = $"Your 10-minute window to book <strong>{expired.TravelPackage?.Destination}</strong> has expired. The opportunity has passed to the next person in queue.",
                    Type = NotificationType.PriorityExpired,
                    TravelPackageId = expired.TravelPackageId
                };
                _context.Notifications.Add(expiredNotification);

                // 🔔 Notify the NEXT waiting user
                var nextInQueue = await _context.WaitingLists
                    .Include(w => w.User)
                    .Include(w => w.TravelPackage)
                    .Where(w => w.TravelPackageId == expired.TravelPackageId 
                             && w.Status == WaitingListStatus.Waiting)
                    .OrderBy(w => w.Position)
                    .ThenBy(w => w.JoinedAt)
                    .FirstOrDefaultAsync();

                if (nextInQueue != null && expired.TravelPackage != null && expired.TravelPackage.AvailableRooms > 0)
                {
                    nextInQueue.Status = WaitingListStatus.Notified;
                    nextInQueue.Notified = true;
                    nextInQueue.NotifiedAt = DateTime.Now;
                    nextInQueue.ExpiresAt = DateTime.Now.AddMinutes(10);

                    var nextNotification = new Notification
                    {
                        UserId = nextInQueue.UserId,
                        Title = $"🎉 Your turn! {expired.TravelPackage.Destination} is available!",
                        Message = $"The previous user's time expired! As you are now #{nextInQueue.Position} in the queue, you have <strong>10 MINUTES</strong> to book <strong>{expired.TravelPackage.Destination}</strong>! ⏰",
                        Type = NotificationType.PackageAvailable,
                        TravelPackageId = expired.TravelPackageId,
                        WaitingListId = nextInQueue.Id,
                        ExpiresAt = nextInQueue.ExpiresAt,
                        ActionUrl = $"/TravelPackages/Details/{expired.TravelPackageId}",
                        ActionText = "Book Now"
                    };
                    _context.Notifications.Add(nextNotification);
                }
            }

            if (expiredEntries.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        // ============ ADD TO CART ============
        [HttpPost]
        public async Task<IActionResult> AddToCart(int packageId)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            const int minimumBookingAge = 18;

            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null)
            {
                TempData["Error"] = "Package not found.";
                return RedirectToAction("Index", "TravelPackages");
            }

            // ✅ Global minimum age restriction (skip for Family packages)
            if (!IsFamilyPackage(package))
            {
                if (!user.DateOfBirth.HasValue)
                {
                    TempData["Error"] = "Please complete your date of birth before booking any package.";
                    return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                }

                var userAge = CalculateAge(user.DateOfBirth.Value);
                if (userAge < minimumBookingAge)
                {
                    TempData["Error"] = $"You must be at least {minimumBookingAge} years old to book any trip.";
                    return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                }
            }

            if (package.AgeLimit > 0)
            {
                if (!user.DateOfBirth.HasValue)
                {
                    TempData["Error"] = "Please complete your date of birth to book age-restricted packages.";
                    return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                }

                var userAge = CalculateAge(user.DateOfBirth.Value);
                if (userAge < package.AgeLimit)
                {
                    TempData["Error"] = $"This package requires minimum age {package.AgeLimit}. Your age is {userAge}.";
                    return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                }
            }

            // Check if already in cart
            var existingItem = await _context.ShoppingCarts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.TravelPackageId == packageId);

            if (existingItem != null)
            {
                TempData["Error"] = "This package is already in your cart.";
                return RedirectToAction("Index", "TravelPackages");
            }

            // ✅ Users CAN buy the same package multiple times (if rooms available)
            // No "already booked" check needed

            // 🔴 NO ROOMS AVAILABLE - must join waiting list
            if (package.AvailableRooms <= 0)
            {
                TempData["Error"] = "No rooms available. Please join the waiting list.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            // ✅ Check if user has PRIORITY (is notified from waiting list)
            var userNotifiedEntry = await _context.WaitingLists
                .FirstOrDefaultAsync(w => w.UserId == userId && 
                                         w.TravelPackageId == packageId &&
                                         w.Status == WaitingListStatus.Notified);

            // Check if user is only WAITING (not notified yet)
            var userWaitingEntry = await _context.WaitingLists
                .FirstOrDefaultAsync(w => w.UserId == userId && 
                                         w.TravelPackageId == packageId &&
                                         w.Status == WaitingListStatus.Waiting);

            if (userWaitingEntry != null && userNotifiedEntry == null)
            {
                // User is waiting but NOT notified - they don't have priority yet
                TempData["Info"] = "⏳ You are in the waiting list. Please wait for your turn to be notified.";
                return RedirectToAction("Index");
            }

            // 🔒 CHECK WAITING LIST PRIORITY - prevent non-waiting-list users from buying if priority is active
            var notifiedUsers = await _context.WaitingLists
                .Where(w => w.TravelPackageId == packageId && w.Status == WaitingListStatus.Notified)
                .ToListAsync();

            if (notifiedUsers.Any() && userNotifiedEntry == null)
            {
                // There are notified users and current user is NOT one of them
                if (package.AvailableRooms <= notifiedUsers.Count)
                {
                    TempData["Error"] = "🔒 This package is currently reserved for users in the waiting list. Please wait for your turn.";
                    return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                }
            }

            // ✅ Add to cart
            var cartItem = new ShoppingCart
            {
                UserId = userId,
                TravelPackageId = packageId,
                AddedAt = DateTime.Now
            };

            _context.ShoppingCarts.Add(cartItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Package added to cart!";
            return RedirectToAction("Index");
        }

        // ============ REMOVE FROM CART ============
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = _userManager.GetUserId(User);

            var cartItem = await _context.ShoppingCarts
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (cartItem != null)
            {
                _context.ShoppingCarts.Remove(cartItem);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Item removed from cart.";
            }

            return RedirectToAction("Index");
        }

        // ============ LEAVE WAITING LIST (decline package) ============
        [HttpPost]
        public async Task<IActionResult> LeaveWaitingList(int waitingListId)
        {
            var userId = _userManager.GetUserId(User);

            var waitingEntry = await _context.WaitingLists
                .Include(w => w.TravelPackage)
                .FirstOrDefaultAsync(w => w.Id == waitingListId && w.UserId == userId);

            if (waitingEntry != null)
            {
                var wasNotified = waitingEntry.Status == WaitingListStatus.Notified;
                var packageId = waitingEntry.TravelPackageId;
                var package = waitingEntry.TravelPackage;

                // Mark as declined/cancelled
                waitingEntry.Status = WaitingListStatus.Declined;
                waitingEntry.RespondedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // 🔔 If this user was notified (had priority), notify the next user
                if (wasNotified && package != null && package.AvailableRooms > 0)
                {
                    var nextInQueue = await _context.WaitingLists
                        .Include(w => w.User)
                        .Where(w => w.TravelPackageId == packageId 
                                 && w.Status == WaitingListStatus.Waiting)
                        .OrderBy(w => w.Position)
                        .ThenBy(w => w.JoinedAt)
                        .FirstOrDefaultAsync();

                    if (nextInQueue != null)
                    {
                        nextInQueue.Status = WaitingListStatus.Notified;
                        nextInQueue.Notified = true;
                        nextInQueue.NotifiedAt = DateTime.Now;
                        nextInQueue.ExpiresAt = DateTime.Now.AddMinutes(10);

                        var notification = new Notification
                        {
                            UserId = nextInQueue.UserId,
                            Title = $"🎉 Your turn! {package.Destination} is available!",
                            Message = $"The previous user declined! As you are now next in queue, you have <strong>10 MINUTES</strong> to book <strong>{package.Destination}</strong>! ⏰",
                            Type = NotificationType.PackageAvailable,
                            TravelPackageId = packageId,
                            WaitingListId = nextInQueue.Id,
                            ExpiresAt = nextInQueue.ExpiresAt,
                            ActionUrl = $"/TravelPackages/Details/{packageId}",
                            ActionText = "Book Now"
                        };
                        _context.Notifications.Add(notification);
                        await _context.SaveChangesAsync();

                        TempData["Info"] = $"You left the waiting list. The next person has been notified.";
                    }
                }
                else
                {
                    TempData["Success"] = "You have been removed from the waiting list.";
                }
            }

            return RedirectToAction("Index");
        }

        // ============ CHECKOUT - VIEW (GET) ============
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);

            var cartItems = await _context.ShoppingCarts
                .Include(c => c.TravelPackage)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            // ⭐ תמיד מגדיר Total
            ViewBag.Total = cartItems.Sum(c => c.FinalPrice);

            return View(cartItems);
        }

        // ============ CHECKOUT - SIMPLE POST (ללא פרמטרים) ============
        [HttpPost]
        public async Task<IActionResult> Checkout(string? dummy)
        {
            // ⭐ זו מתודה פשוטה שרק יוצרת Bookings מסוג Reserved
            // המשתמש ישלם אותם אחר כך ב-MyBookings

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();
            const int minimumBookingAge = 18;

            var cartItems = await _context.ShoppingCarts
                .Include(c => c.TravelPackage)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            // Check 3 bookings limit
            int activeBookingsCount = await _context.Bookings
                .CountAsync(b => b.UserId == userId &&
                                (b.Status == BookingStatus.Reserved || b.Status == BookingStatus.Paid));

            if (activeBookingsCount + cartItems.Count > 3)
            {
                TempData["Error"] = $"You can only have 3 active bookings. You currently have {activeBookingsCount}.";
                return RedirectToAction("Index");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var cartItem in cartItems)
                {
                    var package = await _context.TravelPackages.FindAsync(cartItem.TravelPackageId);

                    if (package == null)
                    {
                        TempData["Error"] = $"Package not found.";
                        await transaction.RollbackAsync();
                        return RedirectToAction("Index");
                    }

                    // ✅ Global minimum age restriction (skip for Family packages)
                    if (!IsFamilyPackage(package))
                    {
                        if (!user.DateOfBirth.HasValue)
                        {
                            TempData["Error"] = "Please complete your date of birth before booking any package.";
                            await transaction.RollbackAsync();
                            return RedirectToAction("Index");
                        }

                        var userAge = CalculateAge(user.DateOfBirth.Value);
                        if (userAge < minimumBookingAge)
                        {
                            TempData["Error"] = $"You must be at least {minimumBookingAge} years old to book any trip.";
                            await transaction.RollbackAsync();
                            return RedirectToAction("Index");
                        }
                    }

                    if (package.AgeLimit > 0)
                    {
                        if (!user.DateOfBirth.HasValue)
                        {
                            TempData["Error"] = "Please complete your date of birth to book age-restricted packages.";
                            await transaction.RollbackAsync();
                            return RedirectToAction("Index");
                        }

                        var userAge = CalculateAge(user.DateOfBirth.Value);
                        if (userAge < package.AgeLimit)
                        {
                            TempData["Error"] = $"Cannot book '{package.Destination}'. Minimum age is {package.AgeLimit}. Your age is {userAge}.";
                            await transaction.RollbackAsync();
                            return RedirectToAction("Index");
                        }
                    }

                    // 🔒 Check if rooms are available for this package
                    if (package.AvailableRooms <= 0)
                    {
                        TempData["Error"] = $"No rooms available for '{package.Destination}'. Please join the waiting list.";
                        await transaction.RollbackAsync();
                        return RedirectToAction("Index");
                    }

                    // 🔒 CHECK WAITING LIST PRIORITY
                    var notifiedUsersCount = await _context.WaitingLists
                        .CountAsync(w => w.TravelPackageId == package.Id && w.Status == WaitingListStatus.Notified);

                    if (notifiedUsersCount > 0 && package.AvailableRooms <= notifiedUsersCount)
                    {
                        // Check if current user is one of the notified users
                        var userIsNotified = await _context.WaitingLists
                            .AnyAsync(w => w.UserId == userId && 
                                          w.TravelPackageId == package.Id && 
                                          w.Status == WaitingListStatus.Notified);

                        if (!userIsNotified)
                        {
                            TempData["Error"] = $"🔒 '{package.Destination}' is reserved for users in the waiting list.";
                            await transaction.RollbackAsync();
                            return RedirectToAction("Index");
                        }
                    }

                    // ⭐ Create booking as RESERVED (not paid yet)
                    // 💡 Room count does NOT decrease here - only on PAYMENT
                    var booking = new Booking
                    {
                        UserId = userId,
                        TravelPackageId = cartItem.TravelPackageId,
                        BookingDate = DateTime.Now,
                        Status = BookingStatus.Reserved  // ⭐ Reserved, not Paid
                    };

                    _context.Bookings.Add(booking);
                }

                _context.ShoppingCarts.RemoveRange(cartItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"✅ Successfully reserved {cartItems.Count} package(s)! Please complete payment.";
                return RedirectToAction("MyBookings", "Bookings");
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "An error occurred during checkout. Please try again.";
                return RedirectToAction("Index");
            }
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }

        private static bool IsFamilyPackage(TravelPackage package)
        {
            return !string.IsNullOrWhiteSpace(package.PackageType) &&
                   package.PackageType.Contains("family", StringComparison.OrdinalIgnoreCase);
        }

        // ============ PROCESS CHECKOUT WITH PAYMENT ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout(
            string cardHolder,
            string cardNumber,
            string expiry,
            string cvv)
        {
            var userId = _userManager.GetUserId(User);

            // Validate payment info
            if (string.IsNullOrWhiteSpace(cardHolder) ||
                string.IsNullOrWhiteSpace(cardNumber) ||
                cardNumber.Replace(" ", "").Length < 13)
            {
                TempData["Error"] = "Invalid payment information.";
                return RedirectToAction("Checkout");
            }

            var cartItems = await _context.ShoppingCarts
                .Include(c => c.TravelPackage)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            // Check 3 bookings limit
            int activeBookingsCount = await _context.Bookings
                .CountAsync(b => b.UserId == userId &&
                                (b.Status == BookingStatus.Reserved || b.Status == BookingStatus.Paid));

            if (activeBookingsCount + cartItems.Count > 3)
            {
                TempData["Error"] = $"You can only have 3 active bookings. You currently have {activeBookingsCount}.";
                return RedirectToAction("Checkout");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var cartItem in cartItems)
                {
                    var package = await _context.TravelPackages.FindAsync(cartItem.TravelPackageId);

                    if (package == null || package.AvailableRooms <= 0)
                    {
                        TempData["Error"] = $"Package '{package?.Destination}' is no longer available.";
                        await transaction.RollbackAsync();
                        return RedirectToAction("Checkout");
                    }

                    // 🔒 CHECK WAITING LIST PRIORITY
                    var notifiedUsersCount = await _context.WaitingLists
                        .CountAsync(w => w.TravelPackageId == package.Id && w.Status == WaitingListStatus.Notified);

                    if (notifiedUsersCount > 0 && package.AvailableRooms <= notifiedUsersCount)
                    {
                        var userIsNotified = await _context.WaitingLists
                            .AnyAsync(w => w.UserId == userId && 
                                          w.TravelPackageId == package.Id && 
                                          w.Status == WaitingListStatus.Notified);

                        if (!userIsNotified)
                        {
                            TempData["Error"] = $"🔒 '{package.Destination}' is reserved for users in the waiting list.";
                            await transaction.RollbackAsync();
                            return RedirectToAction("Checkout");
                        }
                        
                        // Mark user's waiting list entry as booked
                        var userWaitingEntry = await _context.WaitingLists
                            .FirstOrDefaultAsync(w => w.UserId == userId && 
                                                     w.TravelPackageId == package.Id && 
                                                     w.Status == WaitingListStatus.Notified);
                        if (userWaitingEntry != null)
                        {
                            userWaitingEntry.Status = WaitingListStatus.Booked;
                            userWaitingEntry.RespondedAt = DateTime.Now;
                        }
                    }

                    // Create booking as PAID - rooms decrease here
                    var booking = new Booking
                    {
                        UserId = userId,
                        TravelPackageId = cartItem.TravelPackageId,
                        BookingDate = DateTime.Now,
                        Status = BookingStatus.Paid,
                        PaidAt = DateTime.Now
                    };

                    package.AvailableRooms--;  // ✅ Decrease on PAYMENT
                    _context.Bookings.Add(booking);
                }

                _context.ShoppingCarts.RemoveRange(cartItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"✅ Successfully booked {cartItems.Count} package(s)!";
                return RedirectToAction("Index", "Home");
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "An error occurred during checkout. Please try again.";
                return RedirectToAction("Checkout");
            }
        }

        // ============ CLEAR CART ============
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var userId = _userManager.GetUserId(User);

            var cartItems = await _context.ShoppingCarts
                .Where(c => c.UserId == userId)
                .ToListAsync();

            _context.ShoppingCarts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cart cleared.";
            return RedirectToAction("Index");
        }
    }
}