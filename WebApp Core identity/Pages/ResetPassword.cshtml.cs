using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;

namespace WebApp_Core_identity.Pages
{
    [ValidateAntiForgeryToken]
    public class ResetPasswordModel : PageModel
    {
  private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;
   private readonly ILogger<ResetPasswordModel> _logger;

     [BindProperty(SupportsGet = true)]
     public string Token { get; set; } = string.Empty;

  [BindProperty(SupportsGet = true)]
   public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public bool TokenValid { get; set; } = true;
  public string? ErrorMessage { get; set; }

        public ResetPasswordModel(
    UserManager<ApplicationUser> userManager,
       AuditService auditService,
     ILogger<ResetPasswordModel> logger)
  {
       _userManager = userManager;
   _auditService = auditService;
            _logger = logger;
   }

   public async Task<IActionResult> OnGetAsync()
        {
  if (string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(Email))
      {
     TokenValid = false;
      ErrorMessage = "Invalid reset link. The link is missing required information.";
       return Page();
}

       var user = await _userManager.FindByEmailAsync(Email);
   
      if (user == null)
     {
       TokenValid = false;
    ErrorMessage = "Invalid reset link. The account associated with this link could not be found.";
    return Page();
     }

   if (user.PasswordResetToken != Token)
       {
    TokenValid = false;
   ErrorMessage = "Invalid reset link. This link is not valid for this account.";
  return Page();
   }

   if (!user.PasswordResetTokenExpiry.HasValue || user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
       {
      TokenValid = false;
  ErrorMessage = "This reset link has expired. Please request a new password reset link.";
   return Page();
    }

  return Page();
     }

  public async Task<IActionResult> OnPostAsync()
   {
      if (!ModelState.IsValid)
    {
     return Page();
    }

 if (NewPassword != ConfirmPassword)
        {
        ModelState.AddModelError(string.Empty, "Passwords do not match");
    return Page();
  }

     try
 {
     var user = await _userManager.FindByEmailAsync(Email);

   if (user == null || 
      user.PasswordResetToken != Token || 
         !user.PasswordResetTokenExpiry.HasValue ||
        user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
       {
       ModelState.AddModelError(string.Empty, "Invalid or expired reset token. Please request a new password reset.");
    TokenValid = false;
         return Page();
   }

      // Use Identity's built-in password reset
    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
  var result = await _userManager.ResetPasswordAsync(user, resetToken, NewPassword);

     if (result.Succeeded)
          {
      // Clear reset token
 user.PasswordResetToken = null;
       user.PasswordResetTokenExpiry = null;
 await _userManager.UpdateAsync(user);

    // Audit log
  await _auditService.LogAsync(
      user.Id,
 user.Email,
        "PasswordReset",
"Password reset successfully via email",
 HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
       HttpContext.Request.Headers["User-Agent"].ToString(),
 "Success");

  _logger.LogInformation("Password reset successful for {Email}", Email);

         return RedirectToPage("/ResetPasswordConfirmation");
 }

        foreach (var error in result.Errors)
      {
       ModelState.AddModelError(string.Empty, error.Description);
   }
     }
       catch (Exception ex)
   {
 _logger.LogError(ex, "Error resetting password for {Email}", Email);
       ModelState.AddModelError(string.Empty, "An error occurred while resetting your password. Please try again.");
  }

return Page();
    }
    }
}
