using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using WebApp_Core_identity.ViewModels;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Helpers;
using WebApp_Core_Identity.Services;

namespace WebApp_Core_identity.Pages
{
    [ValidateAntiForgeryToken]
    public class LoginModel : PageModel
    {
        [BindProperty]
        public Login LModel { get; set; } = new Login();

        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<LoginModel> logger;
        private readonly AuditService auditService;
        private readonly IEmailService _emailService;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger,
            AuditService auditService,
            IEmailService emailService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.logger = logger;
            this.auditService = auditService;
            this._emailService = emailService;
        }

        public async Task OnGetAsync()
        {
            // Clear any existing authentication to prevent stale session issues
            await HttpContext.SignOutAsync("MyCookieAuth");
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }

        public async Task<IActionResult> OnPostAsync(string? captchaToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // === SECURITY: Input validation ===

                    // Check for SQL injection patterns
                    if (InputValidationHelper.ContainsSqlInjectionPatterns(LModel.Email) ||
                        InputValidationHelper.ContainsSqlInjectionPatterns(LModel.Password))
                    {
                        ModelState.AddModelError("", "Invalid email or password");
                        logger.LogWarning("Potential SQL injection attempt in login from IP: {IP}",
                            HttpContext.Connection.RemoteIpAddress);

                        // Log security event
                        await auditService.LogSecurityEventAsync(
                            "Anonymous",
                            "SQL Injection Attempt",
                            $"Potential SQL injection in login",
                            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                            HttpContext.Request.Headers["User-Agent"].ToString());

                        return Page();
                    }

                    // Validate email format
                    if (!InputValidationHelper.IsValidEmail(LModel.Email))
                    {
                        ModelState.AddModelError("", "Invalid email or password");
                        return Page();
                    }

                    // Sanitize inputs
                    string sanitizedEmail = InputValidationHelper.SanitizeInput(LModel.Email);

                    // === SECURITY: Google reCAPTCHA v3 verification ===
                    // Only verify reCAPTCHA if token is present (after failed attempts)
                    if (!string.IsNullOrEmpty(captchaToken))
                    {
                        var client = new HttpClient();
                        var secretKey = "6LcgHEcsAAAAAHG-99vFR-5dKrz_YBM06Dv15xpG";
                        var response = await client.PostAsync(
                            $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={captchaToken}",
                            null);
                        var jsonString = await response.Content.ReadAsStringAsync();
                        dynamic? captchaResult = JsonConvert.DeserializeObject(jsonString);

                        if (captchaResult?.success != "true" || captchaResult?.score < 0.5)
                        {
                            // Don't reveal bot detection, use generic error
                            ModelState.AddModelError("", "Invalid email or password");
                            logger.LogWarning("reCAPTCHA failed for {Email} from IP: {IP}",
                                sanitizedEmail, HttpContext.Connection.RemoteIpAddress);
                            return Page();
                        }
                    }

                    // === SECURITY: Check if account is locked ===
                    var user = await userManager.FindByEmailAsync(sanitizedEmail);
                    if (user != null)
                    {
                        // Check if account is currently locked
                        if (await userManager.IsLockedOutAsync(user))
                        {
                            // Check if lockout has expired
                            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value <= DateTimeOffset.UtcNow)
                            {
                                // Lockout has expired, reset the lockout
                                await userManager.SetLockoutEndDateAsync(user, null);
                                await userManager.ResetAccessFailedCountAsync(user);
                                logger.LogInformation("Account lockout expired and reset for {Email}", sanitizedEmail);
                            }
                            else
                            {
                                // Still locked out
                                var timeRemaining = user.LockoutEnd.Value - DateTimeOffset.UtcNow;
                                var minutesRemaining = (int)Math.Ceiling(timeRemaining.TotalMinutes);

                                logger.LogWarning("Account locked out for {Email} from IP: {IP}",
                                    sanitizedEmail, HttpContext.Connection.RemoteIpAddress);
                                ModelState.AddModelError("", $"Account is locked. Please try again in {minutesRemaining} minute(s).");

                                // Log failed login
                                await auditService.LogLoginAsync(
                                    user.Id,
                                    sanitizedEmail,
                                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                                    HttpContext.Request.Headers["User-Agent"].ToString(),
                                    false,
                                    "Account locked out");

                                return Page();
                            }
                        }
                    }

                    // === SECURITY: Attempt login with account lockout ===
                    var identityResult = await signInManager.PasswordSignInAsync(
                        sanitizedEmail,
                        LModel.Password,
                        LModel.RememberMe,
                        lockoutOnFailure: true);

                    if (identityResult.Succeeded)
                    {
                        // === 2FA: Generate OTP and redirect to verification page ===

                        // Generate a 6-digit OTP
                        var otp = new Random().Next(100000, 999999).ToString();

                        // Save OTP and User details to Session
                        HttpContext.Session.SetString("OTP", otp);
                        HttpContext.Session.SetString("AuthUserEmail", sanitizedEmail);
                        HttpContext.Session.SetString("AuthUserRemember", LModel.RememberMe.ToString());
                        HttpContext.Session.SetString("OTPTimestamp", DateTime.UtcNow.ToString());

                        // Send OTP Email
                        var subject = "?? Your Login OTP - Fresh Farm Market";
                        var body = $@"
           <html>
                 <body style='font-family: Arial, sans-serif;'>
          <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
            <h2 style='color: #28a745;'>?? Fresh Farm Market</h2>
                 <h3>Your One-Time Password (OTP)</h3>
            <p>Use the following code to complete your login:</p>
     <div style='background-color: #f0f0f0; padding: 20px; text-align: center; border-radius: 5px;'>
           <h1 style='font-size: 32px; letter-spacing: 5px; color: #007bff;'>{otp}</h1>
      </div>
    <p style='color: #dc3545;'><strong>?? This code expires in 5 minutes.</strong></p>
           <p>If you didn't request this code, please ignore this email and ensure your account is secure.</p>
           <hr>
     <p style='font-size: 12px; color: #666;'>This is an automated message. Please do not reply.</p>
            </div>
 </body>
      </html>";

                        await _emailService.SendEmailAsync(sanitizedEmail, subject, body);

                        logger.LogInformation("2FA OTP sent to {Email}", sanitizedEmail);

                        // Sign out from Identity (we'll sign in properly after OTP verification)
                        await signInManager.SignOutAsync();

                        return RedirectToPage("LoginTwoStep");
                    }

                    if (identityResult.IsLockedOut)
                    {
                        var lockedUser = await userManager.FindByEmailAsync(sanitizedEmail);
                        var timeRemaining = lockedUser?.LockoutEnd.HasValue == true
                            ? lockedUser.LockoutEnd.Value - DateTimeOffset.UtcNow
                            : TimeSpan.FromMinutes(3);
                        var minutesRemaining = (int)Math.Ceiling(timeRemaining.TotalMinutes);

                        logger.LogWarning("Account locked out for {Email} from IP: {IP}",
                            sanitizedEmail, HttpContext.Connection.RemoteIpAddress);
                        ModelState.AddModelError("", $"Account is locked due to too many failed attempts. Please try again in {minutesRemaining} minute(s).");

                        // Log failed login
                        if (lockedUser != null)
                        {
                            await auditService.LogLoginAsync(
                                lockedUser.Id,
                                sanitizedEmail,
                                HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                                HttpContext.Request.Headers["User-Agent"].ToString(),
                                false,
                                "Account locked out");
                        }
                    }
                    else
                    {
                        logger.LogWarning("Failed login attempt for {Email} from IP: {IP}",
                            sanitizedEmail, HttpContext.Connection.RemoteIpAddress);
                        ModelState.AddModelError("", "Invalid email or password");

                        // Log failed login
                        await auditService.LogLoginAsync(
                            "Unknown",
                            sanitizedEmail,
                            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                            HttpContext.Request.Headers["User-Agent"].ToString(),
                            false,
                            "Invalid credentials");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Login error for {Email} from IP: {IP}",
                        LModel.Email, HttpContext.Connection.RemoteIpAddress);
                    ModelState.AddModelError("", "An error occurred during login. Please try again.");
                }
            }

            return Page();
        }
    }
}