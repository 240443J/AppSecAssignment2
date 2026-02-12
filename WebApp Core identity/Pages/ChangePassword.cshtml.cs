using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp_Core_identity.ViewModels;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;

namespace WebApp_Core_identity.Pages
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class ChangePasswordModel : PageModel
    {
   private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AuthDbContext _context;
  private readonly AuditService _auditService;
  private readonly IPasswordAgeService _passwordAgeService;
   private readonly ILogger<ChangePasswordModel> _logger;

     [BindProperty]
      public ChangePassword CPModel { get; set; } = new ChangePassword();

      [TempData]
      public string? StatusMessage { get; set; }

    public string? PasswordExpiryWarning { get; set; }
        public int? DaysUntilExpiry { get; set; }

    public ChangePasswordModel(
     UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
     AuthDbContext context,
  AuditService auditService,
          IPasswordAgeService passwordAgeService,
      ILogger<ChangePasswordModel> logger)
        {
   _userManager = userManager;
    _signInManager = signInManager;
       _context = context;
   _auditService = auditService;
         _passwordAgeService = passwordAgeService;
         _logger = logger;
        }

        public async Task OnGetAsync()
{
       var user = await _userManager.GetUserAsync(User);
       if (user != null)
  {
    // Check if password is expiring soon or expired
    var secondsRemaining = await _passwordAgeService.GetSecondsUntilExpiryAsync(user);
  
 if (secondsRemaining <= 60) // Show warning if less than 60 seconds remaining
  {
        if (secondsRemaining <= 0)
       {
         PasswordExpiryWarning = "?? Your password has expired! You must change it now.";
  DaysUntilExpiry = 0;
      }
     else
     {
     PasswordExpiryWarning = $"?? Your password will expire in {Math.Ceiling(secondsRemaining)} second(s). Please change it now.";
              DaysUntilExpiry = (int)Math.Ceiling(secondsRemaining);
      }
  }
        }
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

    // 1. Check Minimum Age
    var (canChange, errorMessage, timeRemaining) = await _passwordAgeService.CanChangePasswordAsync(user);
    if (!canChange)
    {
        ModelState.AddModelError(string.Empty, errorMessage!);
  _logger.LogWarning("User {Email} attempted to change password before minimum age. Time remaining: {TimeRemaining}", 
   user.Email, timeRemaining);
        return Page();
    }

    // 2. Check Password History (Last 2 passwords)
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

    // 3. FIX: Capture the OLD password hash BEFORE changing it
    var oldPasswordHash = user.PasswordHash;

    // 4. Change the password
    var changePasswordResult = await _userManager.ChangePasswordAsync(user, CPModel.CurrentPassword, CPModel.NewPassword);
    
    if (!changePasswordResult.Succeeded)
    {
        foreach (var error in changePasswordResult.Errors)
ModelState.AddModelError(string.Empty, error.Description);
   return Page();
    }

    // 5. FIX: Save the OLD password to history
    if (!string.IsNullOrEmpty(oldPasswordHash))
  {
      _context.PasswordHistories.Add(new PasswordHistory
        {
            UserId = user.Id,
       PasswordHash = oldPasswordHash, // Save the OLD hash
            CreatedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    // 6. Update Password Age & Audit
    await _passwordAgeService.UpdatePasswordChangeDateAsync(user);
    await _auditService.LogPasswordChangeAsync(user.Id, user.Email!, 
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown", 
        HttpContext.Request.Headers["User-Agent"].ToString());

    _logger.LogInformation("User {Email} changed password successfully", user.Email);

    // 7. Refresh Sign-in (Crucial: Do not call UpdateAsync manually after this!)
    await _signInManager.RefreshSignInAsync(user);

    StatusMessage = "Your password has been changed successfully.";
return RedirectToPage();
      }
    }
}
