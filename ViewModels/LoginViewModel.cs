using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace SELENE_STUDIO.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "An email is required")]
        [StringLength(254, ErrorMessage = "Email cannot exceed 254 characters")]
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        [Required(ErrorMessage = "A password is required")]
        [StringLength(64, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 64 characters")]
        public string? Password { get; set; }
    }
}
