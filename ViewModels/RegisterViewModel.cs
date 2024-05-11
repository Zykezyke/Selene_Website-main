using System.ComponentModel.DataAnnotations;

namespace SELENE_STUDIO.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(40, ErrorMessage = "First name cannot exceed 40 characters")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "First name can only contain alphabets and select symbols")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(40, ErrorMessage = "Last name cannot exceed 40 characters")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Last name can only contain alphabets and select symbols")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Contact Number is required")]
        [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Contact Number must contain only digits without '.'")]
        [StringLength(11, MinimumLength = 10, ErrorMessage = "Contact Number must be between 10 and 11 characters")]
        public string? Contact { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Address must be between 1 and 100 characters")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Postal Code is required")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Postal Code must contain only digits")]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "Postal Code cannot exceed 10 characters")]
        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "An email is required")]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "A password is required")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 64 characters")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "You must confirm your password")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
