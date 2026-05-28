using System.ComponentModel.DataAnnotations;

namespace ShieldOps.Models.ViewModels
{
    public class VerifyTokenViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification token is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Token must be exactly 6 digits.")]
        public string Token { get; set; } = string.Empty;
    }
}