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
        private readonly IPasswordAgeService _passwordAgeService;
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
IPasswordAgeService passwordAgeService,
     ILogger<ResetPasswordModel> logger)
        {
    _userManager = userManager;
_auditService = auditService;
      _passwordAgeService = passwordAgeService;
    _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
   {
   _logger.LogInformation("ResetPassword GET - Token: {Token}, Email: {Email}", 
   string.IsNullOrEmpty(Token) ? "EMPTY" : Token.Substring(0, Math.Min(10, Token.Length)) + "...", 
      Email);

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
_logger.LogInformation("ResetPassword POST - Token: {Token}, Email: {Email}", 
        string.IsNullOrEmpty(Token) ? "EMPTY" : "Present", 
            string.IsNullOrEmpty(Email) ? "EMPTY" : Email);

     // Validate passwords match first
            if (NewPassword != ConfirmPassword)
     {
      ModelState.AddModelError(string.Empty, "Passwords do not match");
           TokenValid = true; // Keep form visible
        return Page();
            }

     // Validate password is not empty
  if (string.IsNullOrEmpty(NewPassword))
            {
       ModelState.AddModelError(string.Empty, "Password is required");
      TokenValid = true;
    return Page();
       }

   try
            {
                var user = await _userManager.FindByEmailAsync(Email);

  if (user == null)
   {
          _logger.LogWarning("ResetPassword POST - User not found for email: {Email}", Email);
    ModelState.AddModelError(string.Empty, "Invalid or expired reset token. Please request a new password reset.");
   TokenValid = false;
     return Page();
         }

           // Validate token
         if (user.PasswordResetToken != Token)
    {
 _logger.LogWarning("ResetPassword POST - Token mismatch for user: {Email}", Email);
   ModelState.AddModelError(string.Empty, "Invalid reset token. Please request a new password reset.");
   TokenValid = false;
   return Page();
       }

        // Validate token expiry
       if (!user.PasswordResetTokenExpiry.HasValue || user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
      {
              _logger.LogWarning("ResetPassword POST - Token expired for user: {Email}", Email);
        ModelState.AddModelError(string.Empty, "This reset link has expired. Please request a new password reset link.");
   TokenValid = false;
            return Page();
    }

 // Generate Identity reset token and reset password
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
      var result = await _userManager.ResetPasswordAsync(user, resetToken, NewPassword);

  if (result.Succeeded)
          {
      _logger.LogInformation("Password reset successful for {Email}", Email);

           // FIX: Re-fetch user to avoid OptimisticConcurrencyException
     var updatedUser = await _userManager.FindByEmailAsync(Email);
       if (updatedUser != null)
     {
         updatedUser.PasswordResetToken = null;
      updatedUser.PasswordResetTokenExpiry = null;
        updatedUser.PasswordLastChangedDate = DateTime.UtcNow;
    updatedUser.MustChangePassword = false;
    await _userManager.UpdateAsync(updatedUser);
  }

   // Audit log
  try
     {
 await _auditService.LogAsync(
         updatedUser?.Id ?? user.Id,
       user.Email ?? "Unknown",
         "PasswordReset",
     "Password reset successfully via email",
  HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
 HttpContext.Request.Headers["User-Agent"].ToString(),
 "Success");
          }
    catch (Exception auditEx)
   {
 _logger.LogWarning(auditEx, "Failed to log audit for password reset");
 }

           return RedirectToPage("/ResetPasswordConfirmation");
  }

                // Password reset failed - show errors
    foreach (var error in result.Errors)
          {
   _logger.LogWarning("Password reset error for {Email}: {Error}", Email, error.Description);
     ModelState.AddModelError(string.Empty, error.Description);
     }
      TokenValid = true; // Keep form visible so user can try again
   }
            catch (Exception ex)
            {
    _logger.LogError(ex, "Exception during password reset for {Email}", Email);
         ModelState.AddModelError(string.Empty, "An error occurred while resetting your password. Please try again.");
     TokenValid = true;
   }

        return Page();
    }
  }
}
