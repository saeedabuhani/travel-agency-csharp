


using System.ComponentModel.DataAnnotations;

namespace Travel_Agency.Models
{
    public class TravelPackage
    {
        [Required]
        [Range(1, 60)]
        public int Nights { get; set; }

        public int Id { get; set; }

        // ===== Basic Info =====

        public string? Title { get; set; }


        [Required]
        public string Destination { get; set; } = "";

        [Required]
        public string Country { get; set; } = "";

        [Required]
        [Range(1, 999999)]
        public decimal Price { get; set; }   // Original price

        [Range(0, 999999)]
        public decimal? Discount { get; set; } // Final price after discount (optional)

        [Range(1, 500)]
        public int AvailableRooms { get; set; }

        [Required]
        public string PackageType { get; set; }

        [Range(0, 120)]
        public int AgeLimit { get; set; }


        public string Description { get; set; } = "";

        public string ImageUrl { get; set; }

        // ===== Travel Dates =====

        [Required]
        [Display(Name = "Trip Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Trip End Date")]
        public DateTime EndDate { get; set; }

        // ===== Discount Period (Optional, Max 7 Days) =====

        [Display(Name = "Discount Start Date")]
        public DateTime? DiscountStartDate { get; set; }

        [Display(Name = "Discount End Date")]
        public DateTime? DiscountEndDate { get; set; }

        // ===== Visibility =====
        public bool IsVisible { get; set; } = true;

        // ===== Pricing Helpers =====
        public bool HasDiscount => Discount.HasValue && Discount < Price;

        public bool IsDiscountActive
        {
            get
            {
                if (!HasDiscount)
                    return false;

                if (!DiscountStartDate.HasValue || !DiscountEndDate.HasValue)
                    return false;

                if (DiscountEndDate < DiscountStartDate)
                    return false;

                if ((DiscountEndDate.Value - DiscountStartDate.Value).TotalDays > 7)
                    return false;

                var now = DateTime.Now;
                return now >= DiscountStartDate.Value && now <= DiscountEndDate.Value;
            }
        }

        public decimal EffectivePrice => IsDiscountActive && Discount.HasValue ? Discount.Value : Price;
    }
}
