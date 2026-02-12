using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebApp_Core_identity.ViewModels
{
    public class Register
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
  [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Full Name can only contain letters, spaces, hyphens and apostrophes")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Credit Card Number is required")]
   [RegularExpression(@"^\d{16}$", ErrorMessage = "Credit Card must be exactly 16 digits")]
        [Display(Name = "Credit Card Number")]
        public string CreditCardNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required")]
    [RegularExpression(@"^[689]\d{7}$", ErrorMessage = "Mobile number must be 8 digits starting with 6, 8, or 9")]
        [Display(Name = "Mobile Number")]
 public string MobileNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Delivery Address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        [Display(Name = "Delivery Address")]
     public string DeliveryAddress { get; set; } = string.Empty;

   [Required(ErrorMessage = "Email is required")]
     [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
   [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

      [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
      [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{12,}$", 
         ErrorMessage = "Password must contain at least one uppercase, one lowercase, one digit, and one special character")]
        public string Password { get; set; } = string.Empty;

   [Required(ErrorMessage = "Confirm Password is required")]
        [DataType(DataType.Password)]
[Compare(nameof(Password), ErrorMessage = "Password and confirmation password do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

     [Required(ErrorMessage = "Photo is required")]
    [Display(Name = "Profile Photo")]
        public IFormFile? Photo { get; set; }

        [Required(ErrorMessage = "About Me is required")]
        [StringLength(1000, ErrorMessage = "About Me cannot exceed 1000 characters")]
        [Display(Name = "About Me")]
        public string AboutMe { get; set; } = string.Empty;
    }
}
