


using Travel_Agency.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travel_Agency.Data;
using Travel_Agency.Models;
using Travel_Agency.Services;

namespace Travel_Agency.Controllers
{
    public class BookingsController : Controller
    {
        private readonly TravelAgencyDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookingsController(
            TravelAgencyDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DownloadItinerary(int bookingId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Package)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return NotFound();

            // ✅ Check ownership
            if (booking.UserId != user.Id && !User.IsInRole("Admin"))
                return Unauthorized();

            // ✅ Only allow download for paid bookings
            if (booking.Status != BookingStatus.Paid)
            {
                TempData["Error"] = "Itinerary is only available for paid bookings.";
                return RedirectToAction("MyBookings");
            }

            try
            {
                // ✅ Generate PDF
                var pdfBytes = PdfService.GenerateItinerary(booking, booking.Package, user);

                // ✅ Return as downloadable file
                var fileName = $"Itinerary_{booking.Package.Destination}_{booking.Id}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch
            {
                TempData["Error"] = "Failed to generate itinerary. Please try again.";
                return RedirectToAction("MyBookings");
            }
        }

        // =========================================================
        // 📧 SHARE ITINERARY VIA EMAIL
        // =========================================================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShareItinerary(int bookingId, string friendEmail, string friendName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Validate email
            if (string.IsNullOrWhiteSpace(friendEmail) || !friendEmail.Contains("@"))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return RedirectToAction("MyBookings");
            }

            var booking = await _context.Bookings
                .Include(b => b.Package)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
                return NotFound();

            // Check ownership
            if (booking.UserId != user.Id && !User.IsInRole("Admin"))
                return Unauthorized();

            // Only allow sharing for paid bookings
            if (booking.Status != BookingStatus.Paid)
            {
                TempData["Error"] = "You can only share paid bookings.";
                return RedirectToAction("MyBookings");
            }

            try
            {
                // Generate PDF
                var pdfBytes = PdfService.GenerateItinerary(booking, booking.Package, user);
                var fileName = $"Itinerary_{booking.Package.Destination}_{booking.Id}.pdf";

                // Create email body
                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center;'>
                            <h1 style='color: white; margin: 0;'>✈️ Travel Itinerary</h1>
                        </div>
                        <div style='padding: 30px; background: #f9f9f9;'>
                            <p>Hi {(string.IsNullOrWhiteSpace(friendName) ? "there" : friendName)},</p>
                            <p><strong>{user.FullName}</strong> wanted to share their travel itinerary with you!</p>
                            
                            <div style='background: white; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                                <h2 style='color: #667eea; margin-top: 0;'>📍 {booking.Package.Destination}, {booking.Package.Country}</h2>
                                <p><strong>🗓️ Travel Dates:</strong> {booking.Package.StartDate:dd MMM yyyy} - {booking.Package.EndDate:dd MMM yyyy}</p>
                                <p><strong>🌙 Duration:</strong> {booking.Package.Nights} nights</p>
                                <p><strong>🎯 Package Type:</strong> {booking.Package.PackageType}</p>
                            </div>
                            
                            <p>The full itinerary is attached as a PDF file.</p>
                            <p style='color: #888; font-size: 12px;'>This email was sent via Travel Agency.</p>
                        </div>
                        <div style='background: #333; padding: 20px; text-align: center;'>
                            <p style='color: #888; margin: 0; font-size: 12px;'>🌍 Travel Agency - Making Dreams Come True!</p>
                        </div>
                    </div>
                ";

                // 📧 Try to send real email, fallback to demo mode if SMTP not configured
                bool success;
                try
                {
                    success = await NotificationService.SendWithAttachmentAsync(
                        friendEmail,
                        $"✈️ {user.FullName} shared a travel itinerary with you!",
                        emailBody,
                        pdfBytes,
                        fileName
                    );
                }
                catch
                {
                    // Fallback to demo mode if SMTP fails
                    success = await NotificationService.SendItineraryDemoAsync(
                        friendEmail,
                        user.FullName ?? "A friend",
                        $"{booking.Package.Destination}, {booking.Package.Country}",
                        pdfBytes,
                        fileName
                    );
                }

                if (success)
                {
                    TempData["Success"] = $"✅ Itinerary shared successfully with {friendEmail}!";
                }
                else
                {
                    TempData["Error"] = "Failed to send email. Please try again.";
                }
            }
            catch
            {
                TempData["Error"] = "Failed to share itinerary. Please try again.";
            }

            return RedirectToAction("MyBookings");
        }













        [Authorize]
        [HttpPost]
        public async Task<IActionResult> LeaveWaitingList(int packageId)
        {
            var userId = _userManager.GetUserId(User);

            var entry = await _context.WaitingLists
                .Include(w => w.TravelPackage)
                .FirstOrDefaultAsync(w =>
                    w.TravelPackageId == packageId &&
                    w.UserId == userId &&
                    (w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified));

            if (entry != null)
            {
                var wasNotified = entry.Status == WaitingListStatus.Notified;
                var package = entry.TravelPackage;

                // Mark as declined instead of deleting
                entry.Status = WaitingListStatus.Declined;
                entry.RespondedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // 🔔 If this user had priority (Notified), notify the next user in queue
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
                            Message = $"The previous user declined! You have <strong>10 MINUTES</strong> to book <strong>{package.Destination}</strong>! ⏰",
                            Type = NotificationType.PackageAvailable,
                            TravelPackageId = packageId,
                            WaitingListId = nextInQueue.Id,
                            ExpiresAt = nextInQueue.ExpiresAt,
                            ActionUrl = $"/Bookings/MyBookings"
                        };
                        _context.Notifications.Add(notification);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Success"] = "You have declined this package.";
            }

            return RedirectToAction("MyBookings");
        }






        // ============ GET: הצגת טופס התשלום ============
        [Authorize]
        [HttpGet]  // ⭐ הוספתי [HttpGet] במפורש
        public IActionResult Payment(int bookingId)
        {
            // בדיקה שההזמנה קיימת ושייכת למשתמש
            var userId = _userManager.GetUserId(User);

            var booking = _context.Bookings
                .Include(b => b.Package)
                .FirstOrDefault(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            if (booking.Status != BookingStatus.Reserved)
            {
                TempData["Error"] = "This booking has already been processed.";
                return RedirectToAction("MyBookings");
            }

            // העבר את המידע על ההזמנה ל-View
            ViewBag.Booking = booking;

            return View(new PaymentViewModel { BookingId = bookingId });
        }

        // ============ POST: עיבוד התשלום ============
        [Authorize]
        [HttpPost]  // ⭐ רק POST
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(PaymentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Package)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null)
                return NotFound();

            // Reload ViewBag for view in case of validation errors
            ViewBag.Booking = booking;

            if (booking.UserId != user.Id)
                return Unauthorized();

            if (booking.Status != BookingStatus.Reserved)
            {
                ModelState.AddModelError("", "This booking cannot be paid.");
                return View(model);
            }

            // ✅ Custom validation based on payment method
            if (model.PaymentMethod == "VISA")
            {
                // Validate VISA fields
                if (string.IsNullOrEmpty(model.CardHolder))
                    ModelState.AddModelError("CardHolder", "Card holder name is required");
                
                if (string.IsNullOrEmpty(model.CardNumber))
                    ModelState.AddModelError("CardNumber", "Card number is required");
                else if (!PaymentViewModel.ValidateCardNumber(model.CardNumber))
                    ModelState.AddModelError("CardNumber", "Invalid card number. Please enter a valid 16-digit card number.");
                
                if (string.IsNullOrEmpty(model.Expiry))
                    ModelState.AddModelError("Expiry", "Expiry date is required");
                else if (!PaymentViewModel.ValidateExpiry(model.Expiry))
                    ModelState.AddModelError("Expiry", "Card has expired or invalid expiry format (MM/YY)");
                
                if (string.IsNullOrEmpty(model.CVV))
                    ModelState.AddModelError("CVV", "CVV is required");
                else if (model.CVV.Length < 3 || model.CVV.Length > 4 || !model.CVV.All(char.IsDigit))
                    ModelState.AddModelError("CVV", "CVV must be 3 or 4 digits");
            }
            else if (model.PaymentMethod == "PayPal")
            {
                // Validate PayPal fields
                if (string.IsNullOrEmpty(model.PayPalEmail))
                    ModelState.AddModelError("PayPalEmail", "PayPal email is required");
                else if (!model.PayPalEmail.Contains("@"))
                    ModelState.AddModelError("PayPalEmail", "Please enter a valid email address");
            }
            else
            {
                ModelState.AddModelError("PaymentMethod", "Please select a payment method");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ⭐ Check if room is still available
            if (booking.Package.AvailableRooms <= 0)
            {
                TempData["Error"] = "Sorry, no rooms are currently available. Please try again later or join the waiting list.";
                return RedirectToAction("MyBookings");
            }

            // 🔒 CHECK WAITING LIST PRIORITY
            var userId = _userManager.GetUserId(User);
            var notifiedUsers = await _context.WaitingLists
                .Where(w => w.TravelPackageId == booking.TravelPackageId && w.Status == WaitingListStatus.Notified)
                .OrderBy(w => w.Position)
                .ToListAsync();

            if (notifiedUsers.Any())
            {
                var userEntry = notifiedUsers.FirstOrDefault(w => w.UserId == userId);
                
                if (userEntry != null)
                {
                    // User is in the notified list - check their position
                    var position = notifiedUsers.IndexOf(userEntry);
                    if (position >= booking.Package.AvailableRooms)
                    {
                        TempData["Error"] = "⏳ Please wait for your turn. Users before you in the queue have priority.";
                        return RedirectToAction("MyBookings");
                    }
                    
                    // Mark waiting list entry as booked
                    userEntry.Status = WaitingListStatus.Booked;
                    userEntry.RespondedAt = DateTime.Now;
                }
                else if (booking.Package.AvailableRooms <= notifiedUsers.Count)
                {
                    // User is not in the notified list and all rooms are reserved
                    TempData["Error"] = "🔒 This package is currently reserved for users in the waiting list.";
                    return RedirectToAction("MyBookings");
                }
            }

            // ✅ Payment successful - NOW decrease room count
            booking.Status = BookingStatus.Paid;
            booking.PaidAt = DateTime.Now;
            booking.Package.AvailableRooms--;  // ⭐ ONLY decrease on PAYMENT

            await _context.SaveChangesAsync();

            var paymentMethodDisplay = model.PaymentMethod == "VISA" ? "💳 VISA Card" : "📧 PayPal";
            TempData["Success"] = $"✅ Payment completed successfully via {paymentMethodDisplay}!";
            
            // Send email notification after payment
            try
            {
                await NotificationService.SendAsync(
                    user.Email!,
                    "✅ Booking Confirmed!",
                    $@"
                    <h2 style='color: #2d8a4e;'>Booking Confirmed!</h2>
                    <p>Hello {user.FullName},</p>
                    <p>Your payment has been processed successfully!</p>
                    <div style='background: #f0f8ff; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                        <h3 style='color: #1e3c72; margin-top: 0;'>Booking Details:</h3>
                        <p><strong>📍 Destination:</strong> {booking.Package.Destination}, {booking.Package.Country}</p>
                        <p><strong>📅 Travel Dates:</strong> {booking.Package.StartDate:dd MMM yyyy} - {booking.Package.EndDate:dd MMM yyyy}</p>
                        <p><strong>💰 Amount Paid:</strong> ${booking.Package.EffectivePrice:N2}</p>
                        <p><strong>💳 Payment Method:</strong> {paymentMethodDisplay}</p>
                    </div>
                    <p>You can download your itinerary from your bookings page.</p>
                    <p><a href='https://localhost:7052/Bookings/MyBookings' 
                          style='background: #2d8a4e; color: white; padding: 12px 25px; text-decoration: none; border-radius: 8px; display: inline-block;'>
                        View My Bookings
                    </a></p>
                "
                );
            }
            catch (Exception ex)
            {
                // Log but don't fail the payment
                Console.WriteLine($"Failed to send payment confirmation email: {ex.Message}");
            }
            
            return RedirectToAction("Index", "Home");
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(int packageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
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

            // ✅ Check latest booking date
            var timeFrame = await _context.BookingTimeFrames
                .FirstOrDefaultAsync(tf => tf.TravelPackageId == packageId);

            if (timeFrame != null && !timeFrame.CanBookNow)
            {
                TempData["Error"] = $"Booking is no longer available. The latest booking date was {timeFrame.LatestBookingDate:dd MMM yyyy}.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            // ✅ Users CAN book the same package multiple times (if rooms available)
            // No duplicate booking check needed

            // Check active bookings limit (max 3)
            int activeBookingsCount = await _context.Bookings
                .CountAsync(b => b.UserId == user.Id &&
                                (b.Status == BookingStatus.Reserved || b.Status == BookingStatus.Paid));

            if (activeBookingsCount >= 3)
            {
                TempData["Error"] = "You can only have 3 active bookings at a time.";
                return RedirectToAction("MyBookings");
            }

            using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            try
            {
                var lockedPackage = await _context.TravelPackages
                    .FirstOrDefaultAsync(p => p.Id == packageId);

                if (lockedPackage == null)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Package not found.";
                    return RedirectToAction("Index", "TravelPackages");
                }

                // 🔔 CHECK WAITING LIST PRIORITY
                // Even if rooms are available, check if someone is notified and has priority
                var notifiedUsers = await _context.WaitingLists
                    .Where(w => w.TravelPackageId == packageId && w.Status == WaitingListStatus.Notified)
                    .OrderBy(w => w.Position)
                    .ThenBy(w => w.JoinedAt)
                    .ToListAsync();

                // If there are notified users with priority
                if (notifiedUsers.Any())
                {
                    var userEntry = notifiedUsers.FirstOrDefault(w => w.UserId == user.Id);
                    
                    if (userEntry == null)
                    {
                        // User is NOT in the priority list - cannot book
                        await transaction.RollbackAsync();
                        TempData["Error"] = "🔒 This package is reserved for users in the waiting list. Please wait for your turn.";
                        return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                    }
                    
                    // User IS notified - check if they're first or if there are enough rooms
                    var theirPosition = notifiedUsers.IndexOf(userEntry);
                    if (theirPosition >= lockedPackage.AvailableRooms)
                    {
                        // Not enough rooms for this user's position
                        await transaction.RollbackAsync();
                        TempData["Error"] = "⏳ Please wait for your turn. Users before you in the queue have priority.";
                        return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                    }
                    
                    // ✅ User can book - mark their waiting list entry as booked
                    userEntry.Status = WaitingListStatus.Booked;
                    userEntry.RespondedAt = DateTime.Now;
                }

                // 🟥 אין חדרים – בדיקת תור
                if (lockedPackage.AvailableRooms <= 0)
                {
                    var firstInQueue = await _context.WaitingLists
                        .Where(w => w.TravelPackageId == packageId 
                            && (w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified))
                        .OrderBy(w => w.Position)
                        .ThenBy(w => w.JoinedAt)
                        .FirstOrDefaultAsync();

                    if (firstInQueue == null || firstInQueue.UserId != user.Id)
                    {
                        await transaction.RollbackAsync();
                        TempData["Error"] = "No rooms available. Please join the waiting list.";
                        return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                    }

                    // 🟢 זה תורו → mark as booked
                    firstInQueue.Status = WaitingListStatus.Booked;
                    firstInQueue.RespondedAt = DateTime.Now;
                }

                // 🟢 יצירת הזמנה במצב Reserved
                var booking = new Booking
                {
                    UserId = user.Id,
                    TravelPackageId = packageId,
                    BookingDate = DateTime.Now,
                    Status = BookingStatus.Reserved, // ⭐ עדיין לא שילם!
                    CreatedFromWaitingList = lockedPackage.AvailableRooms <= 0
                };

                // ⭐⭐⭐ **הסרתי** את השורה הזו!
                // lockedPackage.AvailableRooms--; // 🔴 לא כאן!

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ✅ Redirect directly to Payment page!
                return RedirectToAction("Payment", new { bookingId = booking.Id });
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "An unexpected error occurred.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
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


        // ================= USER BOOKINGS =================

        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var bookings = await _context.Bookings
                .Include(b => b.Package)
                .Where(b => b.UserId == user.Id)
                .ToListAsync();

            // Get user's waiting list entries with notifications
            var waitingListEntries = await _context.WaitingLists
                .Include(w => w.TravelPackage)
                .Where(w => w.UserId == user.Id)
                .OrderBy(w => w.JoinedAt)
                .ToListAsync();

            ViewBag.WaitingListEntries = waitingListEntries;
            // ✅ Only show notified entries (Status = Notified) that haven't been booked yet
            var notifiedEntries = waitingListEntries
                .Where(w => w.Status == WaitingListStatus.Notified)
                .ToList();

            // ⭐ Create dictionary for countdown info (packageId -> should show countdown)
            var countdownInfo = new Dictionary<int, bool>();
            foreach (var entry in notifiedEntries)
            {
                var package = await _context.TravelPackages.FindAsync(entry.TravelPackageId);
                if (package != null)
                {
                    var waitingCount = await _context.WaitingLists
                        .CountAsync(w => w.TravelPackageId == entry.TravelPackageId && w.Status == WaitingListStatus.Waiting);
                    
                    // Show countdown only if rooms < waiting users (priority scenario)
                    countdownInfo[entry.TravelPackageId] = package.AvailableRooms < waitingCount && entry.ExpiresAt.HasValue;
                }
            }
            ViewBag.CountdownInfo = countdownInfo;

            ViewBag.NotifiedEntries = notifiedEntries;

            return View(bookings);
        }

        // ================= ADMIN BOOKINGS =================

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Package)
                .Include(b => b.User)
                .ToListAsync();

            return View(bookings);
        }


        [Authorize]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var isAdmin = User.IsInRole("Admin");

            var booking = await _context.Bookings
                .Include(b => b.Package)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
                return NotFound();

            if (booking.UserId != user.Id && !isAdmin)
                return Unauthorized();

            if (booking.Status == BookingStatus.Cancelled)
            {
                TempData["Info"] = "This booking is already cancelled.";
                return RedirectToAction(isAdmin ? "AllBookings" : "MyBookings");
            }

            if (!isAdmin && booking.Status == BookingStatus.Paid)
            {
                // Check cancellation period
                var timeFrame = await _context.BookingTimeFrames
                    .FirstOrDefaultAsync(tf => tf.TravelPackageId == booking.Package.Id);

                if (booking.Package != null)
                {
                    bool canCancel = timeFrame?.CanCancel(booking.Package.StartDate) ?? true;

                    if (!canCancel)
                    {
                        var deadline = timeFrame?.CancellationAllowedUntil ?? 
                                     (timeFrame?.CancellationPeriodDays.HasValue == true 
                                        ? booking.Package.StartDate.AddDays(-timeFrame.CancellationPeriodDays.Value) 
                                        : (DateTime?)null);
                        
                        if (deadline.HasValue)
                        {
                            TempData["Error"] = $"Cancellation is not allowed. The cancellation deadline was {deadline.Value:dd MMM yyyy}.";
                        }
                        else
                        {
                            TempData["Error"] = "Cancellation is not allowed for this booking.";
                        }
                        return RedirectToAction(isAdmin ? "AllBookings" : "MyBookings");
                    }
                }
            }

            // 🔁 Cancel the booking
            booking.Status = BookingStatus.Cancelled;
            // 💡 DO NOT increase AvailableRooms - rooms only decrease on PAYMENT
            // So cancelling a Reserved booking doesn't affect room count
            await _context.SaveChangesAsync();

            // 🔔 Notify the FIRST waiting user with 24h priority
            var next = await _context.WaitingLists
                .Include(w => w.User)
                .Include(w => w.TravelPackage)
                .Where(w => w.TravelPackageId == booking.TravelPackageId 
                    && w.Status == WaitingListStatus.Waiting)
                .OrderBy(w => w.Position)
                .ThenBy(w => w.JoinedAt)
                .FirstOrDefaultAsync();

            if (next != null && next.User != null)
            {
                // Update waiting list entry with priority
                next.Status = WaitingListStatus.Notified;
                next.Notified = true;
                next.NotifiedAt = DateTime.Now;
                next.ExpiresAt = DateTime.Now.AddMinutes(10);  // 10 MINUTES to respond!

                // Create notification
                var notification = new Notification
                {
                    UserId = next.UserId,
                    Title = $"🎉 Your turn! {next.TravelPackage?.Destination} is available!",
                    Message = $"A room has become available for <strong>{next.TravelPackage?.Destination}</strong>! " +
                              $"As you are #{next.Position} in the queue, you have <strong>10 MINUTES</strong> to book before the opportunity passes to the next person! ⏰" +
                              $"<br/><br/>Price: <strong>${next.TravelPackage?.EffectivePrice.ToString("N0")}</strong> | Duration: {next.TravelPackage?.Nights} nights",
                    Type = NotificationType.PackageAvailable,
                    TravelPackageId = next.TravelPackageId,
                    WaitingListId = next.Id,
                    ExpiresAt = next.ExpiresAt,
                    ActionUrl = $"/TravelPackages/Details/{next.TravelPackageId}",
                    ActionText = "Book Now"
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                // Send email
                if (!string.IsNullOrEmpty(next.User.Email))
                {
                    try
                    {
                        await NotificationService.SendAsync(
                            next.User.Email,
                            "🎉 Your turn to book!",
                            $@"
                            <h2 style='color: #2d8a4e;'>Great news, {next.User.FullName}!</h2>
                            <p>A room is now available for <strong>{next.TravelPackage?.Destination}</strong>.</p>
                            <p style='color: #e74c3c; font-weight: bold;'>⏰ URGENT: You only have 10 MINUTES to book!</p>
                            <p><a href='https://localhost:5001/TravelPackages/Details/{next.TravelPackageId}' 
                                  style='background: #2d8a4e; color: white; padding: 12px 25px; text-decoration: none; border-radius: 8px; display: inline-block;'>
                                Book Now
                            </a></p>
                        ");
                    }
                    catch { /* Email sending is optional */ }
                }
            }

            TempData["Success"] = "Booking cancelled successfully.";
            return RedirectToAction(isAdmin ? "AllBookings" : "MyBookings");
        }


[Authorize]
        public async Task<IActionResult> JoinWaitingList(int packageId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            const int minimumBookingAge = 18;

            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null)
            {
                TempData["Error"] = "Package not found.";
                return RedirectToAction("Index", "TravelPackages");
            }

            if (package.AvailableRooms > 0)
            {
                TempData["Error"] = "Rooms are still available. Please book directly instead of joining the waiting list.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            // ✅ Global minimum age restriction for waiting list (skip for Family packages)
            if (!IsFamilyPackage(package))
            {
                if (!user.DateOfBirth.HasValue)
                {
                    TempData["Error"] = "Please complete your date of birth before joining the waiting list.";
                    return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                }

                var userAge = CalculateAge(user.DateOfBirth.Value);
                if (userAge < minimumBookingAge)
                {
                    TempData["Error"] = $"You must be at least {minimumBookingAge} years old to join the waiting list.";
                    return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                }
            }

            if (package.AgeLimit > 0)
            {
                if (!user.DateOfBirth.HasValue)
                {
                    TempData["Error"] = "Please complete your date of birth to join the waiting list for age-restricted packages.";
                    return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                }

                var userAge = CalculateAge(user.DateOfBirth.Value);
                if (userAge < package.AgeLimit)
                {
                    TempData["Error"] = $"This package requires minimum age {package.AgeLimit}. Your age is {userAge}.";
                    return RedirectToAction("Details", "TravelPackages", new { id = packageId });
                }
            }

            // ❌ בדיקה אם כבר בתור (active entries only)
            bool alreadyInList = await _context.WaitingLists
                .AnyAsync(w => w.UserId == userId 
                    && w.TravelPackageId == packageId
                    && (w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified));

            if (alreadyInList)
            {
                TempData["Message"] = "You are already in the waiting list.";
                return RedirectToAction("Details", "TravelPackages", new { id = packageId });
            }

            // ✅ חישוב Position - כמה אנשים פעילים בתור?
            int currentCount = await _context.WaitingLists
                .CountAsync(w => w.TravelPackageId == packageId 
                    && (w.Status == WaitingListStatus.Waiting || w.Status == WaitingListStatus.Notified));

            var entry = new WaitingList
            {
                UserId = userId,
                TravelPackageId = packageId,
                JoinedAt = DateTime.Now,
                Position = currentCount + 1,
                Notified = false,
                Status = WaitingListStatus.Waiting  // ⭐ Explicit status
            };

            _context.WaitingLists.Add(entry);

            // Create notification for user
            var notification = new Notification
            {
                UserId = userId,
                Title = $"📋 Added to waiting list for {package.Destination}",
                Message = $"You are #{entry.Position} in the queue for <strong>{package.Destination}, {package.Country}</strong>. We'll notify you immediately when a room becomes available!",
                Type = NotificationType.WaitingListJoined,
                TravelPackageId = packageId
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            TempData["Message"] = $"✅ You joined the waiting list. Your position: #{entry.Position}";
            return RedirectToAction("Details", "TravelPackages", new { id = packageId });
        }





    }


}



