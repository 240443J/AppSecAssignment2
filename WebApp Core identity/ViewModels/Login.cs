using System.ComponentModel.DataAnnotations;

namespace WebApp_Core_identity.ViewModels
{
    public class Login
    {
        [Required]
        [EmailAddress]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}