using System.Net;
using System.Net.Mail;

namespace Travel_Agency.Services
{
    public static class NotificationService
    {
        // ============================================================
        // 🔧 SMTP CONFIGURATION - CHANGE THESE TO YOUR VALUES!
        // ============================================================
        // For Gmail:
        // 1. Go to https://myaccount.google.com/apppasswords
        // 2. Create an App Password (requires 2FA enabled)
        // 3. Use that password here
        // ============================================================
        
        // ✅ CONFIGURED WITH REAL GMAIL CREDENTIALS
        private static readonly string FromEmail = "saied442001@gmail.com";
        private static readonly string FromName = "Travel Agency";
        private static readonly string SmtpHost = "smtp.gmail.com";
        private static readonly int SmtpPort = 587;
        private static readonly string SmtpUser = "saied442001@gmail.com";
        private static readonly string SmtpPass = "REDACTED";  // App Password (no spaces)
        
        // Check if SMTP is configured (true if not using placeholder values)
        private static bool IsSmtpConfigured => 
            !SmtpUser.Contains("your-email") && !SmtpPass.Contains("your-app-password") && SmtpPass.Length >= 16;

        // Simple notification (console)
        public static void Send(string message)
        {
            Console.WriteLine($"[NOTIFICATION] {message}");
        }

        // Send email (async)
        public static async Task SendAsync(string toEmail, string subject, string body)
        {
            try
            {
                using var client = new SmtpClient(SmtpHost, SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(SmtpUser, SmtpPass)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(FromEmail, FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"✅ [EMAIL SENT] To: {toEmail}, Subject: {subject}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [EMAIL ERROR] {ex.Message}");
                throw; // Re-throw so caller knows it failed
            }
        }

        // ✅ Send email with PDF attachment
        public static async Task<bool> SendWithAttachmentAsync(
            string toEmail, 
            string subject, 
            string body, 
            byte[] pdfBytes, 
            string fileName)
        {
            // Check if SMTP is configured
            if (!IsSmtpConfigured)
            {
                Console.WriteLine("⚠️ [SMTP NOT CONFIGURED] Using demo mode instead.");
                Console.WriteLine("   To enable real email, configure credentials in NotificationService.cs");
                return await SendItineraryDemoAsync(toEmail, "User", "Travel Package", pdfBytes, fileName);
            }

            try
            {
                using var client = new SmtpClient(SmtpHost, SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(SmtpUser, SmtpPass),
                    Timeout = 30000 // 30 seconds timeout
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(FromEmail, FromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                // Create attachment from bytes
                using var ms = new MemoryStream(pdfBytes);
                var attachment = new Attachment(ms, fileName, "application/pdf");
                mailMessage.Attachments.Add(attachment);

                // Send the email
                await client.SendMailAsync(mailMessage);
                
                Console.WriteLine($"✅ [EMAIL WITH PDF SENT] To: {toEmail}, File: {fileName}");
                return true;
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"❌ [SMTP ERROR] {ex.Message}");
                Console.WriteLine($"   Status Code: {ex.StatusCode}");
                Console.WriteLine($"   💡 Make sure to configure SMTP credentials in NotificationService.cs");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [EMAIL ERROR] {ex.Message}");
                return false;
            }
        }

        // Demo mode for testing
        public static async Task<bool> SendItineraryDemoAsync(
            string toEmail,
            string senderName,
            string destination,
            byte[] pdfBytes,
            string fileName)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("[DEMO EMAIL] Itinerary Share Request");
            Console.WriteLine($"  From: {senderName}");
            Console.WriteLine($"  To: {toEmail}");
            Console.WriteLine($"  Destination: {destination}");
            Console.WriteLine($"  Attachment: {fileName} ({pdfBytes.Length} bytes)");
            Console.WriteLine("========================================");
            
            await Task.Delay(500);
            return true;
        }
    }
}
