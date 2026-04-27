using Microsoft.EntityFrameworkCore;
using Travel_Agency.Data;
using Travel_Agency.Models;

namespace Travel_Agency.Services
{
    public class TripReminderService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;
        private readonly ILogger<TripReminderService> _logger;

        public TripReminderService(IServiceProvider serviceProvider, ILogger<TripReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Trip Reminder Service is starting.");

            // Run every hour
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(1));

            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TravelAgencyDbContext>();

                await CheckAndSendReminders(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Trip Reminder Service.");
            }
        }

        private async Task CheckAndSendReminders(TravelAgencyDbContext context)
        {
            // Get all paid bookings with upcoming trips
            var upcomingBookings = await context.Bookings
                .Include(b => b.Package)
                .Include(b => b.User)
                .Where(b => b.Status == BookingStatus.Paid && 
                           b.Package != null &&
                           b.Package.StartDate > DateTime.Now &&
                           b.Package.StartDate <= DateTime.Now.AddDays(30)) // Check next 30 days
                .ToListAsync();

            foreach (var booking in upcomingBookings)
            {
                if (booking.Package == null || booking.User == null)
                    continue;

                // Get booking time frame for this package
                var timeFrame = await context.BookingTimeFrames
                    .FirstOrDefaultAsync(tf => tf.TravelPackageId == booking.Package.Id);

                if (timeFrame?.ReminderDaysBeforeDeparture == null)
                    continue; // No reminder configured

                var daysUntilTrip = (booking.Package.StartDate - DateTime.Now).Days;

                // Check if we should send reminder today
                if (daysUntilTrip == timeFrame.ReminderDaysBeforeDeparture.Value)
                {
                    // Check if we already sent this reminder
                    var existingReminder = await context.Notifications
                        .AnyAsync(n => n.UserId == booking.UserId &&
                                      n.TravelPackageId == booking.Package.Id &&
                                      n.Type == NotificationType.GeneralMessage &&
                                      n.Message.Contains("departure reminder") &&
                                      n.CreatedAt.Date == DateTime.Today);

                    if (!existingReminder)
                    {
                        // Send reminder notification
                        var notification = new Notification
                        {
                            UserId = booking.UserId,
                            Title = $"✈️ Trip Reminder: {booking.Package.Destination}",
                            Message = $"Your trip to <strong>{booking.Package.Destination}, {booking.Package.Country}</strong> is departing in <strong>{daysUntilTrip} days</strong>! " +
                                     $"Don't forget to prepare for your adventure! 🎒",
                            Type = NotificationType.GeneralMessage,
                            TravelPackageId = booking.Package.Id,
                            ActionUrl = "/Bookings/MyBookings",
                            ActionText = "View Booking"
                        };

                        context.Notifications.Add(notification);

                        // Send email reminder
                        try
                        {
                            await NotificationService.SendAsync(
                                booking.User.Email!,
                                $"✈️ Trip Reminder: {booking.Package.Destination}",
                                $@"
                                <h2 style='color: #2d8a4e;'>Hello {booking.User.FullName}!</h2>
                                <p>This is a friendly reminder that your trip to <strong>{booking.Package.Destination}, {booking.Package.Country}</strong> is departing in <strong>{daysUntilTrip} days</strong>!</p>
                                <div style='background: #f0f8ff; padding: 20px; border-radius: 10px; margin: 20px 0;'>
                                    <h3 style='color: #1e3c72; margin-top: 0;'>Trip Details:</h3>
                                    <p><strong>📍 Destination:</strong> {booking.Package.Destination}, {booking.Package.Country}</p>
                                    <p><strong>📅 Departure Date:</strong> {booking.Package.StartDate:dd MMM yyyy}</p>
                                    <p><strong>📅 Return Date:</strong> {booking.Package.EndDate:dd MMM yyyy}</p>
                                    <p><strong>🌙 Duration:</strong> {booking.Package.Nights} nights</p>
                                </div>
                                <p>Make sure you have everything ready for your adventure! 🎒</p>
                                <p><a href='https://localhost:7052/Bookings/MyBookings' 
                                      style='background: #2d8a4e; color: white; padding: 12px 25px; text-decoration: none; border-radius: 8px; display: inline-block;'>
                                    View My Bookings
                                </a></p>
                                <p style='color: #666; font-size: 12px;'>Travel Agency - Your Adventure Awaits</p>
                            "
                            );

                            _logger.LogInformation($"Sent reminder to {booking.User.Email} for trip {booking.Package.Destination}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send email reminder to {booking.User.Email}");
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Trip Reminder Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
