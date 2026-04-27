using System.ComponentModel.DataAnnotations;

namespace Travel_Agency.Models
{
    public class WebsiteReview
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public AppUser? User { get; set; }

        [Required]
        [Range(1, 5)]
        [Display(Name = "Rating")]
        public int Rating { get; set; } // 1–5 ⭐

        [Display(Name = "Comment")]
        [StringLength(1000)]
        public string? Comment { get; set; }

        [Display(Name = "Review Type")]
        public WebsiteReviewType ReviewType { get; set; } = WebsiteReviewType.BookingExperience;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsPublished { get; set; } = true; // Admin can hide inappropriate reviews
    }

    public enum WebsiteReviewType
    {
        BookingExperience,    // Review of the booking/purchasing experience
        WebsiteExperience,    // General website experience
        ServiceExperience     // Overall service experience
    }
}
