using System.Net;
using System.Net.Mail;

namespace Travel_Agency.Services
{
    public static class NotificationService
    {
        // SMTP settings are read from environment variables (never commit credentials):
        //   SMTP_USER  - sender address (e.g. your Gmail address)
        //   SMTP_PASS  - an app password for that account
        //   SMTP_HOST  - optional, defaults to smtp.gmail.com
        //   SMTP_PORT  - optional, defaults to 587
        // If SMTP_USER or SMTP_PASS is missing, email sending falls back to demo mode.
        private static readonly string SmtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? "";
        private static readonly string SmtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? "";
        private static readonly string SmtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
        private static readonly int SmtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
        private static readonly string FromEmail = SmtpUser;
        private static readonly string FromName = "Travel Agency";

        private static bool IsSmtpConfigured =>
            !string.IsNullOrWhiteSpace(SmtpUser) && !string.IsNullOrWhiteSpace(SmtpPass);

        // Simple notification (console)
        public static void Send(string message)
        {
            Console.WriteLine($"[NOTIFICATION] {message}");
        }

        // Send email (async)
        public static async Task SendAsync(string toEmail, string subject, string body)
        {
            if (!IsSmtpConfigured)
            {
                Console.WriteLine($"[SMTP NOT CONFIGURED] Email to {toEmail} skipped: {subject}");
                return;
            }

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
                Console.WriteLine("   To enable real email, set the SMTP_USER and SMTP_PASS environment variables");
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
