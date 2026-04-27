//using System.ComponentModel.DataAnnotations;

//namespace Travel_Agency.ViewModels
//{
//    public class RegisterViewModel
//    {
//        [Required]
//        [Display(Name = "Full Name")]
//        public string FullName { get; set; }

//        [Required]
//        [EmailAddress]
//        [Display(Name = "Email")]
//        public string Email { get; set; } = "";

//        [Required]
//        [DataType(DataType.Password)]
//        [Display(Name = "Password")]
//        public string Password { get; set; } = "";

//        [Required]
//        [DataType(DataType.Password)]
//        [Compare("Password")]
//        [Display(Name = "Confirm Password")]
//        public string ConfirmPassword { get; set; } = "";
//    }
//}



using System.ComponentModel.DataAnnotations;

namespace Travel_Agency.ViewModels
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [RegularExpression(
            @"^[^@\s]+@gmail\.com$",
            ErrorMessage = "Email must be a valid Gmail address (example@gmail.com)"
        )]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters and include uppercase, lowercase, number, and one of ! @ # $ % ^ & *.")]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateOfBirth.HasValue && DateOfBirth.Value.Date > DateTime.Today)
            {
                yield return new ValidationResult(
                    "Date of birth cannot be in the future.",
                    new[] { nameof(DateOfBirth) });
            }
        }
    }
}
