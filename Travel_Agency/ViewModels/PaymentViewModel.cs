using System.ComponentModel.DataAnnotations;

namespace Travel_Agency.ViewModels
{
    public class PaymentViewModel
    {
        public int BookingId { get; set; }

        // Payment Method Selection
        [Required(ErrorMessage = "Please select a payment method")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "VISA"; // "VISA" or "PayPal"

        // ============================================
        // 💳 VISA/Card Payment Fields
        // ============================================
        [Display(Name = "Card Holder Name")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
        public string? CardHolder { get; set; }

        [Display(Name = "Card Number")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must be exactly 16 digits")]
        public string? CardNumber { get; set; }

        [Display(Name = "Expiry Date")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Expiry must be in MM/YY format")]
        public string? Expiry { get; set; }

        [Display(Name = "CVV")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits")]
        public string? CVV { get; set; }

        // ============================================
        // 📧 PayPal Payment Fields
        // ============================================
        [Display(Name = "PayPal Email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string? PayPalEmail { get; set; }

        // ============================================
        // ✅ Custom Validation
        // ============================================
        public bool ValidatePayment()
        {
            if (PaymentMethod == "VISA")
            {
                return !string.IsNullOrEmpty(CardHolder) &&
                       !string.IsNullOrEmpty(CardNumber) &&
                       !string.IsNullOrEmpty(Expiry) &&
                       !string.IsNullOrEmpty(CVV) &&
                       ValidateCardNumber(CardNumber) &&
                       ValidateExpiry(Expiry);
            }
            else if (PaymentMethod == "PayPal")
            {
                return !string.IsNullOrEmpty(PayPalEmail);
            }
            return false;
        }

        // Luhn Algorithm for card validation
        public static bool ValidateCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length != 16)
                return false;

            // Remove spaces if any
            cardNumber = cardNumber.Replace(" ", "");

            // Check if all characters are digits
            if (!long.TryParse(cardNumber, out _))
                return false;

            // Luhn Algorithm
            int sum = 0;
            bool isAlternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (isAlternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                isAlternate = !isAlternate;
            }

            return sum % 10 == 0;
        }

        public static bool ValidateExpiry(string expiry)
        {
            if (string.IsNullOrEmpty(expiry))
                return false;

            var parts = expiry.Split('/');
            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out int month) || !int.TryParse(parts[1], out int year))
                return false;

            if (month < 1 || month > 12)
                return false;

            // Convert 2-digit year to 4-digit
            year += 2000;

            var expiryDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            return expiryDate >= DateTime.Today;
        }
    }
}
