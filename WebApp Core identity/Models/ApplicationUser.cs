using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApp_Core_Identity.Model
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Full Name can only contain letters, spaces, hyphens, and apostrophes.")]
        [PersonalData]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Credit Card Number is required")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Credit Card Number must be 16 digits.")]
        [PersonalData]
        public string CreditCardNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required")]
        [PersonalData]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required")]
        [RegularExpression(@"^[689]\d{7}$", ErrorMessage = "Mobile Number must be 8 digits and start with 6, 8, or 9.")]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "Mobile Number must be exactly 8 digits.")]
        [PersonalData]
        public string MobileNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Delivery Address is required")]
        [StringLength(500, ErrorMessage = "Delivery Address cannot exceed 500 characters.")]
        [PersonalData]
        public string DeliveryAddress { get; set; } = string.Empty;

        [PersonalData]
        public string? PhotoPath { get; set; }

        [StringLength(1000, ErrorMessage = "About Me cannot exceed 1000 characters.")]
        [PersonalData]
        public string? AboutMe { get; set; }

        // Multi-device login tracking
        public string? CurrentSessionId { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? LastLoginIP { get; set; }

        // Password reset properties
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        // === PASSWORD AGE PROPERTIES ===
        
        /// <summary>
        /// Date and time when the password was last changed
        /// </summary>
        public DateTime? PasswordLastChangedDate { get; set; }

        /// <summary>
        /// Indicates if the user must change their password on next login (password expired)
        /// </summary>
        public bool MustChangePassword { get; set; } = false;

        /// <summary>
        /// Date when the user was last warned about password expiration
        /// </summary>
        public DateTime? PasswordExpiryWarningDate { get; set; }
    }
}
