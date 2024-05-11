using System.ComponentModel.DataAnnotations;

namespace SELENE_STUDIO.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
