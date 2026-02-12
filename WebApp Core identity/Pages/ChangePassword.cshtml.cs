using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp_Core_identity.ViewModels;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;

namespace WebApp_Core_identity.Pages
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AuthDbContext _context;
  private readonly AuditService _auditService;
   private readonly ILogger<ChangePasswordModel> _logger;

     [BindProperty]
        public ChangePassword CPModel { get; set; } = new ChangePassword();

      [TempData]
      public string? StatusMessage { get; set; }

        public ChangePasswordModel(
     UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
     AuthDbContext context,
  AuditService auditService,
      ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
    _signInManager = signInManager;
       _context = context;
            _auditService = auditService;
         _logger = logger;
        }

        public void OnGet()
        {
        }

     public async Task<IActionResult> OnPostAsync()
        {
     if (!ModelState.IsValid)
   {
         return Page();
}

      var user = await _userManager.GetUserAsync(User);
       if (user == null)
        {
        return RedirectToPage("/Login");
            }

            try
   {
      // Verify current password
       var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, CPModel.CurrentPassword);
     if (!isCurrentPasswordValid)
          {
      ModelState.AddModelError("CPModel.CurrentPassword", "Current password is incorrect");
  return Page();
       }

   // Check password history (last 2 passwords)
var passwordHistories = _context.PasswordHistories
     .Where(ph => ph.UserId == user.Id)
    .OrderByDescending(ph => ph.CreatedDate)
        .Take(2)
               .ToList();

     var passwordHasher = new PasswordHasher<ApplicationUser>();
      foreach (var history in passwordHistories)
    {
                var result = passwordHasher.VerifyHashedPassword(user, history.PasswordHash, CPModel.NewPassword);
        if (result == PasswordVerificationResult.Success)
  {
ModelState.AddModelError("CPModel.NewPassword", "You cannot reuse your last 2 passwords");
              return Page();
    }
  }

        // Change password
    var changePasswordResult = await _userManager.ChangePasswordAsync(user, CPModel.CurrentPassword, CPModel.NewPassword);
           if (!changePasswordResult.Succeeded)
      {
        foreach (var error in changePasswordResult.Errors)
           {
         ModelState.AddModelError(string.Empty, error.Description);
      }
        return Page();
}

         // Save old password to history
      var newPasswordHash = _userManager.PasswordHasher.HashPassword(user, CPModel.NewPassword);
  _context.PasswordHistories.Add(new PasswordHistory
       {
  UserId = user.Id,
            PasswordHash = user.PasswordHash!, // Save old password
         CreatedDate = DateTime.UtcNow
       });
  await _context.SaveChangesAsync();

      // Update user's password hash
             user.PasswordHash = newPasswordHash;
     await _userManager.UpdateAsync(user);

   // Audit log
       await _auditService.LogPasswordChangeAsync(
            user.Id,
   user.Email!,
         HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
  HttpContext.Request.Headers["User-Agent"].ToString());

                _logger.LogInformation("User {Email} changed password successfully", user.Email);

        // Re-sign in with new password
      await _signInManager.RefreshSignInAsync(user);

      StatusMessage = "Your password has been changed successfully.";
              return RedirectToPage();
            }
 catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", user.Id);
       ModelState.AddModelError(string.Empty, "An error occurred while changing your password. Please try again.");
        return Page();
  }
        }
    }
}
