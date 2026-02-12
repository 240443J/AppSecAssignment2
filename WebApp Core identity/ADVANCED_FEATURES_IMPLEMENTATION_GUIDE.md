# ?? Advanced Security Features Implementation Guide

**Project:** Fresh Farm Market Web Application  
**Target:** Advanced Security Features (10% additional points)  
**Difficulty:** Medium to Advanced  
**Estimated Time:** 2-6 hours per feature

---

## ?? Overview

This guide provides detailed implementation steps for three advanced security features:

1. **Reset Password (Email/SMS)** - 40% effort, requires external service
2. **Minimum Password Age** - 30% effort, straightforward implementation
3. **Maximum Password Age** - 30% effort, requires background job

---

# 1?? Reset Password (Email Link)

**Estimated Time:** 2-3 hours  
**Complexity:** Medium  
**External Dependencies:** SMTP service (Gmail, SendGrid, Azure Communication Services)

## ?? Overview

Allow users to reset forgotten passwords via email verification link.

## ?? Prerequisites

### Option A: Gmail SMTP (Free for testing)
```bash
# No NuGet packages needed - built into .NET
# Just need Gmail account with App Password
```

### Option B: SendGrid (Production recommended)
```bash
dotnet add package SendGrid
```

### Option C: Azure Communication Services
```bash
dotnet add package Azure.Communication.Email
```

---

## ?? Implementation Steps

### Step 1: Add Properties to ApplicationUser

**File:** `Models/ApplicationUser.cs`

```csharp
public class ApplicationUser : IdentityUser
{
    // ...existing properties...
    
    // Password Reset Properties
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
}
```

### Step 2: Create Email Service

**File:** `Services/EmailService.cs`

```csharp
using System.Net;
using System.Net.Mail;

namespace WebApp_Core_Identity.Services
{
    public interface IEmailService
  {
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
    }

    public class EmailService : IEmailService
    {
private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

      public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
            _configuration = configuration;
    _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            try
  {
        var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
 var fromEmail = _configuration["Email:FromEmail"];
      var fromPassword = _configuration["Email:FromPassword"];

    using var smtpClient = new SmtpClient(smtpHost, smtpPort)
   {
   EnableSsl = true,
   Credentials = new NetworkCredential(fromEmail, fromPassword)
       };

    var mailMessage = new MailMessage
                {
  From = new MailAddress(fromEmail!, "Fresh Farm Market"),
         Subject = "Password Reset Request",
     Body = $@"
           <html>
          <body>
      <h2>Password Reset Request</h2>
      <p>You requested to reset your password. Click the link below to proceed:</p>
      <p><a href='{resetLink}'>Reset Password</a></p>
         <p>This link will expire in 30 minutes.</p>
                  <p>If you didn't request this, please ignore this email.</p>
   <br/>
          <p>Best regards,<br/>Fresh Farm Market Team</p>
    </body>
 </html>",
           IsBodyHtml = true
          };

        mailMessage.To.Add(toEmail);

           await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Password reset email sent to {Email}", toEmail);
            }
        catch (Exception ex)
      {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
         throw;
            }
        }
    }
}
```

### Step 3: Register Service in Program.cs

**File:** `Program.cs`

```csharp
// Add after other service registrations
builder.Services.AddScoped<IEmailService, EmailService>();
```

### Step 4: Create ForgotPassword Page

**File:** `Pages/ForgotPassword.cshtml.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;
using System.Security.Cryptography;
using System.Text;

namespace WebApp_Core_identity.Pages
{
    [ValidateAntiForgeryToken]
    public class ForgotPasswordModel : PageModel
    {
   private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ForgotPasswordModel> _logger;
     private readonly AuditService _auditService;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

      public ForgotPasswordModel(
       UserManager<ApplicationUser> userManager,
      IEmailService emailService,
    ILogger<ForgotPasswordModel> logger,
     AuditService auditService)
   {
            _userManager = userManager;
          _emailService = emailService;
            _logger = logger;
      _auditService = auditService;
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

  try
        {
   var user = await _userManager.FindByEmailAsync(Email);

          // Always show success message (don't reveal if email exists)
    StatusMessage = "If an account with that email exists, a password reset link has been sent.";

           if (user != null)
      {
    // Generate secure token
          var token = GenerateSecureToken();
     user.PasswordResetToken = token;
  user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

              await _userManager.UpdateAsync(user);

        // Generate reset link
     var resetLink = Url.PageLink("/ResetPassword", values: new { token, email = Email });
        var fullResetLink = $"{Request.Scheme}://{Request.Host}{resetLink}";

    // Send email
       await _emailService.SendPasswordResetEmailAsync(Email, fullResetLink);

        // Audit log
        await _auditService.LogAsync(
       user.Id,
     user.Email,
      "PasswordResetRequest",
       "Password reset email sent",
      HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
  HttpContext.Request.Headers["User-Agent"].ToString());

         _logger.LogInformation("Password reset requested for {Email}", Email);
}
 else
             {
           // Log attempt for non-existent email
      await _auditService.LogSecurityEventAsync(
           "Unknown",
          "PasswordResetAttempt",
          $"Password reset attempted for non-existent email: {Email}",
                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
         HttpContext.Request.Headers["User-Agent"].ToString());
           }

   return RedirectToPage("/ForgotPasswordConfirmation");
      }
  catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset for {Email}", Email);
      ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
  return Page();
            }
        }

        private string GenerateSecureToken()
  {
            using var rng = RandomNumberGenerator.Create();
            var tokenData = new byte[32];
  rng.GetBytes(tokenData);
    return Convert.ToBase64String(tokenData).Replace("+", "-").Replace("/", "_");
        }
  }
}
```

**File:** `Pages/ForgotPassword.cshtml`

```razor
@page
@model WebApp_Core_identity.Pages.ForgotPasswordModel
@{
    ViewData["Title"] = "Forgot Password";
}

<div class="container mt-5">
    <div class="row justify-content-center">
<div class="col-md-6">
            <div class="card">
        <div class="card-header">
      <h2>Forgot Password</h2>
         </div>
         <div class="card-body">
         <p>Enter your email address and we'll send you a link to reset your password.</p>
        
      <form method="post" asp-antiforgery="true">
              <div asp-validation-summary="All" class="text-danger mb-3"></div>
    
   <div class="mb-3">
        <label asp-for="Email" class="form-label">Email Address</label>
       <input asp-for="Email" type="email" class="form-control" required />
   <span asp-validation-for="Email" class="text-danger"></span>
   </div>
   
               <button type="submit" class="btn btn-primary w-100">Send Reset Link</button>
    </form>
   
              <div class="mt-3 text-center">
          <a asp-page="/Login">Back to Login</a>
  </div>
    </div>
 </div>
 </div>
    </div>
</div>
```

### Step 5: Create ResetPassword Page

**File:** `Pages/ResetPassword.cshtml.cs`

```csharp
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
        return Page();
        }

          var user = await _userManager.FindByEmailAsync(Email);
            if (user == null || 
        user.PasswordResetToken != Token || 
           user.PasswordResetTokenExpiry < DateTime.UtcNow)
       {
 TokenValid = false;
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
        user.PasswordResetTokenExpiry < DateTime.UtcNow)
     {
         ModelState.AddModelError(string.Empty, "Invalid or expired reset token");
            TokenValid = false;
 return Page();
    }

  // Reset password
       var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, NewPassword);

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
 HttpContext.Request.Headers["User-Agent"].ToString());

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
      ModelState.AddModelError(string.Empty, "An error occurred. Please try again.");
            }

  return Page();
        }
    }
}
```

**File:** `Pages/ResetPassword.cshtml`

```razor
@page
@model WebApp_Core_identity.Pages.ResetPasswordModel
@{
    ViewData["Title"] = "Reset Password";
}

<div class="container mt-5">
    <div class="row justify-content-center">
        <div class="col-md-6">
        <div class="card">
       <div class="card-header">
  <h2>Reset Password</h2>
     </div>
       <div class="card-body">
        @if (!Model.TokenValid)
        {
     <div class="alert alert-danger">
  <strong>Invalid or Expired Link</strong>
  <p>This password reset link is invalid or has expired. Please request a new one.</p>
<a asp-page="/ForgotPassword" class="btn btn-primary">Request New Link</a>
       </div>
    }
    else
        {
    <form method="post" asp-antiforgery="true">
      <div asp-validation-summary="All" class="text-danger mb-3"></div>
 
      <input type="hidden" asp-for="Token" />
    <input type="hidden" asp-for="Email" />
        
  <div class="mb-3">
         <label asp-for="NewPassword" class="form-label">New Password</label>
    <input asp-for="NewPassword" type="password" class="form-control" required 
      minlength="12" />
        <small class="form-text text-muted">
             Min 12 characters with uppercase, lowercase, number and special character
  </small>
   </div>
      
 <div class="mb-3">
          <label asp-for="ConfirmPassword" class="form-label">Confirm Password</label>
             <input asp-for="ConfirmPassword" type="password" class="form-control" required />
 </div>
        
              <button type="submit" class="btn btn-primary w-100">Reset Password</button>
                </form>
          }
      </div>
            </div>
     </div>
    </div>
</div>
```

### Step 6: Add Link to Login Page

**File:** `Pages/Login.cshtml`

Add this after the login form:

```razor
<div class="text-center mt-3">
    <a asp-page="/ForgotPassword">Forgot your password?</a>
</div>
```

### Step 7: Configuration (appsettings.json)

**IMPORTANT:** Never commit credentials to source control!

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "FromEmail": "your-email@gmail.com",
    "FromPassword": "your-app-password"
  }
}
```

**For Gmail:**
1. Enable 2FA on your Google account
2. Generate App Password: https://myaccount.google.com/apppasswords
3. Use app password (not your regular password)

### Step 8: Create Migration

```bash
dotnet ef migrations add AddPasswordResetFields
dotnet ef database update
```

---

# 2?? Minimum Password Age (30 minutes)

**Estimated Time:** 1 hour  
**Complexity:** Easy  
**External Dependencies:** None

## ?? Overview

Prevent users from changing password too frequently (e.g., can't change within 30 minutes).

## ?? Implementation Steps

### Step 1: Add Property to ApplicationUser

**File:** `Models/ApplicationUser.cs`

```csharp
public class ApplicationUser : IdentityUser
{
 // ...existing properties...
    
    // Password Age Properties
    public DateTime? LastPasswordChangeDate { get; set; }
}
```

### Step 2: Update ChangePassword Logic

**File:** `Pages/ChangePassword.cshtml.cs`

Add this check BEFORE the password history check:

```csharp
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

   // === MINIMUM PASSWORD AGE CHECK (30 MINUTES) ===
        if (user.LastPasswordChangeDate.HasValue)
        {
    var timeSinceLastChange = DateTime.UtcNow - user.LastPasswordChangeDate.Value;
 var minimumAge = TimeSpan.FromMinutes(30);

       if (timeSinceLastChange < minimumAge)
            {
       var remainingTime = minimumAge - timeSinceLastChange;
              var minutesRemaining = (int)Math.Ceiling(remainingTime.TotalMinutes);

      ModelState.AddModelError(string.Empty, 
     $"You must wait {minutesRemaining} more minute(s) before changing your password again.");
  
                _logger.LogWarning("User {Email} attempted password change too soon (minimum age violation)", 
           user.Email);

        // Audit log
     await _auditService.LogSecurityEventAsync(
         user.Id,
  "MinimumPasswordAgeViolation",
      $"Attempted password change within 30-minute minimum age period",
               HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
 HttpContext.Request.Headers["User-Agent"].ToString());

           return Page();
}
        }

        // Check password history (last 2 passwords)
        var passwordHistories = _context.PasswordHistories
   .Where(ph => ph.UserId == user.Id)
            .OrderByDescending(ph => ph.CreatedDate)
    .Take(2)
  .ToList();

   // ...rest of existing code...

     // After successful password change, update the timestamp
        user.LastPasswordChangeDate = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // ...rest of existing code...
    }
  catch (Exception ex)
    {
        // ...existing error handling...
    }
}
```

### Step 3: Update Registration

**File:** `Pages/Register.cshtml.cs`

Set initial password change date:

```csharp
var user = new ApplicationUser()
{
    UserName = RModel.Email,
    Email = RModel.Email,
    // ...other properties...
    LastPasswordChangeDate = DateTime.UtcNow  // Add this line
};
```

### Step 4: Create Migration

```bash
dotnet ef migrations add AddMinimumPasswordAge
dotnet ef database update
```

### Step 5: Add UI Indicator

**File:** `Pages/ChangePassword.cshtml`

Add this above the form:

```razor
@if (Model.User?.LastPasswordChangeDate.HasValue == true)
{
    var timeSinceChange = DateTime.UtcNow - Model.User.LastPasswordChangeDate.Value;
 var canChange = timeSinceChange.TotalMinutes >= 30;
    
    <div class="alert @(canChange ? "alert-info" : "alert-warning")">
      <i class="bi bi-clock"></i>
 <strong>Last password change:</strong> 
     @Model.User.LastPasswordChangeDate.Value.ToString("MMM dd, yyyy HH:mm") UTC
        @if (!canChange)
   {
    var remaining = 30 - (int)timeSinceChange.TotalMinutes;
            <br/><small>You can change your password in @remaining minute(s).</small>
        }
    </div>
}
```

---

# 3?? Maximum Password Age (Force change after X days)

**Estimated Time:** 2-3 hours  
**Complexity:** Medium  
**External Dependencies:** Background job scheduler (Hangfire or HostedService)

## ?? Overview

Force users to change password after a certain period (e.g., 90 days).

## ?? Prerequisites

### Option A: Hangfire (Recommended for complex scheduling)

```bash
dotnet add package Hangfire
dotnet add package Hangfire.SqlServer
```

### Option B: Built-in HostedService (Simple, no dependencies)

No additional packages needed.

---

## ?? Implementation (Option B: HostedService)

### Step 1: Add Property to ApplicationUser

**File:** `Models/ApplicationUser.cs`

```csharp
public class ApplicationUser : IdentityUser
{
    // ...existing properties...
    
    // Maximum Password Age Properties
    public DateTime? LastPasswordChangeDate { get; set; }
    public bool MustChangePassword { get; set; } = false;
    public DateTime? PasswordExpiryDate { get; set; }
}
```

### Step 2: Create Background Service

**File:** `Services/PasswordExpiryBackgroundService.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp_Core_Identity.Model;

namespace WebApp_Core_Identity.Services
{
  public class PasswordExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PasswordExpiryBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Check daily
     private readonly int _passwordMaxAgeDays = 90; // 90 days

        public PasswordExpiryBackgroundService(
 IServiceProvider serviceProvider,
     ILogger<PasswordExpiryBackgroundService> logger)
        {
          _serviceProvider = serviceProvider;
            _logger = logger;
        }

 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
       _logger.LogInformation("Password Expiry Background Service started");

            while (!stoppingToken.IsCancellationRequested)
          {
  try
      {
       await CheckPasswordExpiry();
          }
             catch (Exception ex)
   {
    _logger.LogError(ex, "Error in Password Expiry Background Service");
            }

  await Task.Delay(_checkInterval, stoppingToken);
 }
        }

        private async Task CheckPasswordExpiry()
        {
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
          var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();

            var users = await userManager.Users.ToListAsync();
          var now = DateTime.UtcNow;
            var expiryThreshold = now.AddDays(-_passwordMaxAgeDays);

  foreach (var user in users)
            {
                if (user.LastPasswordChangeDate.HasValue)
      {
  if (user.LastPasswordChangeDate.Value <= expiryThreshold && !user.MustChangePassword)
      {
       // Password has expired
            user.MustChangePassword = true;
     user.PasswordExpiryDate = now;
       await userManager.UpdateAsync(user);

        // Audit log
    await auditService.LogAsync(
          user.Id,
        user.Email,
                "PasswordExpired",
             $"Password expired after {_passwordMaxAgeDays} days. User must change password.",
       "System",
 "BackgroundService",
        "Warning");

      _logger.LogWarning("Password expired for user {Email}", user.Email);
             }
      }
         }

            _logger.LogInformation("Password expiry check completed. Checked {Count} users", users.Count);
        }
    }
}
```

### Step 3: Register Background Service

**File:** `Program.cs`

```csharp
// Add before var app = builder.Build();
builder.Services.AddHostedService<PasswordExpiryBackgroundService>();
```

### Step 4: Create Middleware to Enforce Password Change

**File:** `Middleware/PasswordExpiryMiddleware.cs`

```csharp
using Microsoft.AspNetCore.Identity;
using WebApp_Core_Identity.Model;

namespace WebApp_Core_Identity.Middleware
{
    public class PasswordExpiryMiddleware
    {
        private readonly RequestDelegate _next;

        public PasswordExpiryMiddleware(RequestDelegate next)
        {
         _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
     {
      if (context.User.Identity?.IsAuthenticated == true)
    {
                // Skip password-related pages
      var path = context.Request.Path.Value?.ToLower() ?? "";
       if (path.Contains("/changepassword") || 
    path.Contains("/logout") || 
         path.Contains("/login") ||
               path.Contains("/api"))
            {
         await _next(context);
         return;
     }

      var user = await userManager.GetUserAsync(context.User);
  if (user != null && user.MustChangePassword)
    {
       // Redirect to change password page
    context.Response.Redirect("/ChangePassword?expired=true");
           return;
      }
}

        await _next(context);
 }
    }

    public static class PasswordExpiryMiddlewareExtensions
    {
     public static IApplicationBuilder UsePasswordExpiry(this IApplicationBuilder builder)
        {
    return builder.UseMiddleware<PasswordExpiryMiddleware>();
}
}
}
```

### Step 5: Register Middleware

**File:** `Program.cs`

```csharp
// Add AFTER app.UseAuthentication() and BEFORE app.UseAuthorization()
app.UseAuthentication();
app.UsePasswordExpiry(); // Add this line
app.UseAuthorization();
```

### Step 6: Update ChangePassword Page

**File:** `Pages/ChangePassword.cshtml.cs`

Add at the beginning of OnGet():

```csharp
public async Task OnGetAsync(bool expired = false)
{
    var user = await _userManager.GetUserAsync(User);
    if (user != null && expired && user.MustChangePassword)
    {
  StatusMessage = "Your password has expired. You must change it to continue.";
    }
}
```

After successful password change, clear the flag:

```csharp
// After successful password change
user.LastPasswordChangeDate = DateTime.UtcNow;
user.MustChangePassword = false;
user.PasswordExpiryDate = null;
await _userManager.UpdateAsync(user);
```

### Step 7: Update UI

**File:** `Pages/ChangePassword.cshtml`

```razor
@page
@model WebApp_Core_identity.Pages.ChangePasswordModel
@{
    ViewData["Title"] = "Change Password";
}

<div class="container mt-5">
    <div class="row justify-content-center">
        <div class="col-md-6">
   @if (Model.User?.MustChangePassword == true)
      {
   <div class="alert alert-danger">
<i class="bi bi-exclamation-triangle"></i>
         <strong>Password Expired!</strong>
             <p>Your password has expired and must be changed to continue using your account.</p>
     <small>Passwords expire after 90 days for security.</small>
</div>
    }
   
            @if (Model.User?.LastPasswordChangeDate.HasValue == true)
            {
    var daysSinceChange = (DateTime.UtcNow - Model.User.LastPasswordChangeDate.Value).Days;
       var daysRemaining = 90 - daysSinceChange;
    
 <div class="alert @(daysRemaining > 14 ? "alert-info" : "alert-warning")">
               <i class="bi bi-clock-history"></i>
       <strong>Password Status:</strong>
     <br/>
              Last changed: @Model.User.LastPasswordChangeDate.Value.ToString("MMM dd, yyyy")
        <br/>
      @if (daysRemaining > 0)
      {
        <small>Expires in @daysRemaining days</small>
                 }
    </div>
            }
      
            <!-- Rest of form -->
        </div>
 </div>
</div>
```

### Step 8: Create Migration

```bash
dotnet ef migrations add AddPasswordExpiryFields
dotnet ef database update
```

---

## ?? Feature Comparison

| Feature | Complexity | Time | External Deps | User Impact |
|---------|-----------|------|---------------|-------------|
| **Reset Password** | Medium | 2-3 hrs | SMTP | Low (emergency only) |
| **Min Password Age** | Easy | 1 hr | None | Low (30 min wait) |
| **Max Password Age** | Medium | 2-3 hrs | None (Option B) | Medium (90-day cycle) |

---

## ?? Recommended Implementation Order

1. **Minimum Password Age** (1 hour) - Quick win, easy implementation
2. **Reset Password** (2-3 hours) - High value, user convenience
3. **Maximum Password Age** (2-3 hours) - Long-term security, background job

---

## ? Testing Checklist

### Reset Password
- [ ] Request reset link for valid email
- [ ] Request reset link for invalid email (should not reveal)
- [ ] Click reset link and change password
- [ ] Try to use expired token (30 min)
- [ ] Try to use token twice
- [ ] Verify email is sent correctly

### Minimum Password Age
- [ ] Change password successfully
- [ ] Try to change again immediately (should fail)
- [ ] Wait 30 minutes and try again (should succeed)
- [ ] Check audit log for violations

### Maximum Password Age
- [ ] Manually set user.LastPasswordChangeDate to 91 days ago
- [ ] Wait for background service to run (or restart app)
- [ ] Try to access any page (should redirect to change password)
- [ ] Change password (should clear flag and allow access)
- [ ] Verify background service logs

---

## ?? Configuration Reference

### appsettings.json (Recommended structure)

```json
{
  "Security": {
    "Password": {
      "MinimumAgeDays": 0,
      "MinimumAgeMinutes": 30,
"MaximumAgeDays": 90,
      "ResetTokenExpiryMinutes": 30,
      "HistoryCount": 2
    }
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
 "FromEmail": "noreply@freshfarmmarket.com",
    "FromPassword": "USE-ENVIRONMENT-VARIABLE"
  }
}
```

**IMPORTANT:** Use environment variables or Azure Key Vault for production credentials!

---

## ?? Quick Start Commands

```bash
# Install required packages (if using Hangfire)
dotnet add package Hangfire
dotnet add package Hangfire.SqlServer

# Create migrations
dotnet ef migrations add AddPasswordResetFields
dotnet ef migrations add AddMinimumPasswordAge  
dotnet ef migrations add AddPasswordExpiryFields

# Apply migrations
dotnet ef database update

# Build and run
dotnet build
dotnet run
```

---

## ?? Need Help?

**Common Issues:**

1. **Email not sending?**
   - Check firewall settings
   - Verify SMTP credentials
   - Enable "Less secure app access" (Gmail) or use App Password

2. **Background service not running?**
   - Check logs: ILogger output
   - Verify service is registered in Program.cs
   - Restart application

3. **Migration errors?**
   - Delete last migration: `dotnet ef migrations remove`
   - Check database connection string
   - Ensure AuthDbContext is updated

---

**Good luck with your implementation! ??**
