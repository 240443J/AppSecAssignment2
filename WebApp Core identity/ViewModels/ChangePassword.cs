using System.ComponentModel.DataAnnotations;

namespace WebApp_Core_identity.ViewModels
{
    public class ChangePassword
    {
        [Required(ErrorMessage = "Current password is required")]
  [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
 public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters")]
[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{12,}$", 
 ErrorMessage = "Password must contain at least one uppercase, one lowercase, one digit, and one special character")]
        [Display(Name = "New Password")]
      public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
   [DataType(DataType.Password)]
[Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match")]
      [Display(Name = "Confirm New Password")]
   public string ConfirmPassword { get; set; } = string.Empty;
    }
}
