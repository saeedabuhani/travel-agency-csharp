using Microsoft.EntityFrameworkCore;
using Travel_Agency.Data;
using Travel_Agency.Models;

namespace Travel_Agency.Services
{
    public class WaitingListService
    {
        private readonly TravelAgencyDbContext _context;
        
        // ⏰ Time limit for users to respond to availability notification (10 minutes!)
        public static readonly int PRIORITY_MINUTES = 10;

        public WaitingListService(TravelAgencyDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Called when admin adds rooms to a package.
        /// Notifies waiting users based on priority queue rules.
        /// </summary>
        public async Task<int> NotifyWaitingUsers(int packageId, int newRoomsAdded)
        {
            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null) return 0;

            // Get all users still waiting (not yet notified or already expired/declined)
            var waitingUsers = await _context.WaitingLists
                .Include(w => w.User)
                .Where(w => w.TravelPackageId == packageId 
                         && w.Status == WaitingListStatus.Waiting)
                .OrderBy(w => w.Position)  // FIFO - First In First Out
                .ThenBy(w => w.JoinedAt)
                .ToListAsync();

            if (!waitingUsers.Any()) return 0;

            int availableRooms = package.AvailableRooms;
            int notifiedCount = 0;

            // Check if there are enough rooms for everyone
            bool enoughForAll = availableRooms >= waitingUsers.Count;

            if (enoughForAll)
            {
                // ✅ Notify ALL waiting users - no priority needed
                foreach (var waiting in waitingUsers)
                {
                    await NotifyUser(waiting, package, isPriority: false);
                    notifiedCount++;
                }
            }
            else
            {
                // 🔢 Priority Queue - Only notify users up to available rooms
                // Each user gets 24 hours to respond
                var usersToNotify = waitingUsers.Take(availableRooms).ToList();
                
                foreach (var waiting in usersToNotify)
                {
                    await NotifyUser(waiting, package, isPriority: true);
                    notifiedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return notifiedCount;
        }

        /// <summary>
        /// Sends notification to a waiting user
        /// </summary>
        private async Task NotifyUser(WaitingList waiting, TravelPackage package, bool isPriority)
        {
            var expiresAt = isPriority ? DateTime.Now.AddMinutes(PRIORITY_MINUTES) : (DateTime?)null;
            
            // Update waiting list entry
            waiting.Status = WaitingListStatus.Notified;
            waiting.Notified = true;
            waiting.NotifiedAt = DateTime.Now;
            waiting.ExpiresAt = expiresAt;

            // Create notification for user
            var notification = new Notification
            {
                UserId = waiting.UserId,
                Title = isPriority 
                    ? $"🎉 Your turn! {package.Destination} is available!" 
                    : $"🎉 Great news! {package.Destination} is available!",
                Message = isPriority
                    ? $"A room has become available for your waiting package <strong>{package.Destination}</strong>! " +
                      $"As you are #{waiting.Position} in the queue, you have <strong>{PRIORITY_MINUTES} MINUTES</strong> to book before the opportunity passes to the next person! ⏰" +
                      $"<br/><br/>Price: <strong>${package.EffectivePrice:N0}</strong> | Duration: {package.Nights} nights"
                    : $"Great news! Rooms are now available for <strong>{package.Destination}</strong>! " +
                      $"There are enough rooms for all waiting users, so you can book at your convenience. " +
                      $"<br/><br/>Price: <strong>${package.EffectivePrice:N0}</strong> | Duration: {package.Nights} nights",
                Type = NotificationType.PackageAvailable,
                TravelPackageId = package.Id,
                WaitingListId = waiting.Id,
                ExpiresAt = expiresAt,
                ActionUrl = $"/TravelPackages/Details/{package.Id}",
                ActionText = "Book Now"
            };

            _context.Notifications.Add(notification);
            
            // Send email notification
            if (waiting.User?.Email != null)
            {
                var emailBody = $@"
                    <h2 style='color: #2d8a4e;'>🎉 {notification.Title}</h2>
                    <p>{notification.Message.Replace("<strong>", "<b>").Replace("</strong>", "</b>")}</p>
                    {(isPriority ? $"<p style='color: #e74c3c; font-weight: bold;'>⏰ URGENT: You only have {PRIORITY_MINUTES} MINUTES to book!</p>" : "")}
                    <p><a href='https://localhost:5001/TravelPackages/Details/{package.Id}' 
                          style='background: #2d8a4e; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                        Book Now
                    </a></p>
                    <p style='color: #666; font-size: 12px;'>Travel Agency - Your Adventure Awaits</p>
                ";

                try
                {
                    await NotificationService.SendAsync(
                        waiting.User.Email,
                        notification.Title,
                        emailBody
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send email to {waiting.User.Email}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Check and expire old notifications, move to next in queue
        /// Should be called periodically (e.g., on page load or via background job)
        /// </summary>
        public async Task ProcessExpiredNotifications(int packageId)
        {
            var expiredEntries = await _context.WaitingLists
                .Where(w => w.TravelPackageId == packageId 
                         && w.Status == WaitingListStatus.Notified
                         && w.ExpiresAt.HasValue 
                         && w.ExpiresAt < DateTime.Now)
                .ToListAsync();

            foreach (var entry in expiredEntries)
            {
                entry.Status = WaitingListStatus.Expired;
                entry.RespondedAt = DateTime.Now;

                // Create expiry notification
                var notification = new Notification
                {
                    UserId = entry.UserId,
                    Title = "⏰ Your booking priority has expired",
                    Message = $"Your {PRIORITY_MINUTES}-minute window to book has expired. The opportunity has been passed to the next person in the queue.",
                    Type = NotificationType.PriorityExpired,
                    TravelPackageId = entry.TravelPackageId
                };
                _context.Notifications.Add(notification);
            }

            if (expiredEntries.Any())
            {
                await _context.SaveChangesAsync();
                
                // Notify next users in queue
                var package = await _context.TravelPackages.FindAsync(packageId);
                if (package != null && package.AvailableRooms > 0)
                {
                    await NotifyNextInQueue(packageId, expiredEntries.Count);
                }
            }
        }

        /// <summary>
        /// Notify next users in queue after someone expires/declines
        /// </summary>
        private async Task NotifyNextInQueue(int packageId, int count)
        {
            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package == null) return;

            var nextInQueue = await _context.WaitingLists
                .Include(w => w.User)
                .Where(w => w.TravelPackageId == packageId && w.Status == WaitingListStatus.Waiting)
                .OrderBy(w => w.Position)
                .ThenBy(w => w.JoinedAt)
                .Take(Math.Min(count, package.AvailableRooms))
                .ToListAsync();

            foreach (var waiting in nextInQueue)
            {
                await NotifyUser(waiting, package, isPriority: true);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// User declines the booking opportunity
        /// </summary>
        public async Task<bool> DeclineBooking(int waitingListId, string userId)
        {
            var entry = await _context.WaitingLists
                .Include(w => w.TravelPackage)
                .FirstOrDefaultAsync(w => w.Id == waitingListId && w.UserId == userId);

            if (entry == null || entry.Status != WaitingListStatus.Notified)
                return false;

            entry.Status = WaitingListStatus.Declined;
            entry.RespondedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Notify next in queue
            if (entry.TravelPackage != null)
            {
                await NotifyNextInQueue(entry.TravelPackageId, 1);
            }

            return true;
        }

        /// <summary>
        /// Called when user successfully books from waiting list
        /// </summary>
        public async Task MarkAsBooked(int waitingListId, string userId)
        {
            var entry = await _context.WaitingLists
                .FirstOrDefaultAsync(w => w.Id == waitingListId && w.UserId == userId);

            if (entry != null)
            {
                entry.Status = WaitingListStatus.Booked;
                entry.RespondedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Add user to waiting list with proper queue position
        /// </summary>
        public async Task<WaitingList> AddToWaitingList(string userId, int packageId)
        {
            // Get current max position for this package
            var maxPosition = await _context.WaitingLists
                .Where(w => w.TravelPackageId == packageId)
                .MaxAsync(w => (int?)w.Position) ?? 0;

            var entry = new WaitingList
            {
                UserId = userId,
                TravelPackageId = packageId,
                JoinedAt = DateTime.Now,
                Position = maxPosition + 1,
                Status = WaitingListStatus.Waiting
            };

            _context.WaitingLists.Add(entry);
            await _context.SaveChangesAsync();

            // Create confirmation notification
            var package = await _context.TravelPackages.FindAsync(packageId);
            if (package != null)
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = $"📋 Added to waiting list for {package.Destination}",
                    Message = $"You are #{entry.Position} in the queue for <strong>{package.Destination}</strong>. " +
                              $"We'll notify you immediately when a room becomes available!",
                    Type = NotificationType.WaitingListJoined,
                    TravelPackageId = packageId
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            return entry;
        }

        /// <summary>
        /// Get unread notification count for a user
        /// </summary>
        public async Task<int> GetUnreadCount(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && n.Status == NotificationStatus.Unread);
        }

        /// <summary>
        /// Estimate availability based on historical waiting list resolution times.
        /// Returns null if there isn't enough data.
        /// </summary>
        public async Task<TimeSpan?> EstimateAvailability(int packageId, int? position)
        {
            var resolvedEntries = await _context.WaitingLists
                .Where(w => w.TravelPackageId == packageId &&
                            w.RespondedAt.HasValue &&
                            (w.Status == WaitingListStatus.Booked ||
                             w.Status == WaitingListStatus.Declined ||
                             w.Status == WaitingListStatus.Expired))
                .ToListAsync();

            if (!resolvedEntries.Any())
                return null;

            var avgMinutes = resolvedEntries
                .Average(w => (w.RespondedAt!.Value - w.JoinedAt).TotalMinutes);

            if (avgMinutes <= 0)
                return null;

            int effectivePosition = position.HasValue && position.Value > 0 ? position.Value : 1;
            var estimatedMinutes = avgMinutes * effectivePosition;

            return TimeSpan.FromMinutes(estimatedMinutes);
        }
    }
}
