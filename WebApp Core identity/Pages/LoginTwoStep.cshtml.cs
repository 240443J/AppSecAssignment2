using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;

namespace WebApp_Core_identity.Pages
{
    public class LoginTwoStepModel : PageModel
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly AuditService auditService;
        private readonly ILogger<LoginTwoStepModel> _logger;

        [BindProperty]
        public string OTPInput { get; set; } = string.Empty;

        public LoginTwoStepModel(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 AuditService auditService,
                                 ILogger<LoginTwoStepModel> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.auditService = auditService;
            this._logger = logger;
        }

        public IActionResult OnGet()
        {
            // Check if OTP exists in session
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("OTP")))
            {
                return RedirectToPage("/Login");
            }

            // Check if OTP has expired (5 minutes)
            var otpTimestamp = HttpContext.Session.GetString("OTPTimestamp");
            if (!string.IsNullOrEmpty(otpTimestamp))
            {
                if (DateTime.TryParse(otpTimestamp, out var timestamp))
                {
                    if (DateTime.UtcNow - timestamp > TimeSpan.FromMinutes(5))
                    {
                        // OTP expired, clear session and redirect
                        HttpContext.Session.Remove("OTP");
                        HttpContext.Session.Remove("AuthUserEmail");
                        HttpContext.Session.Remove("OTPTimestamp");
                        return RedirectToPage("/Login");
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var sessionOTP = HttpContext.Session.GetString("OTP");
            var email = HttpContext.Session.GetString("AuthUserEmail");
            var otpTimestamp = HttpContext.Session.GetString("OTPTimestamp");

            // Check if session data exists
            if (string.IsNullOrEmpty(sessionOTP) || string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Session expired. Please login again.");
                return RedirectToPage("/Login");
            }

            // Check if OTP has expired (5 minutes)
            if (!string.IsNullOrEmpty(otpTimestamp))
            {
                if (DateTime.TryParse(otpTimestamp, out var timestamp))
                {
                    if (DateTime.UtcNow - timestamp > TimeSpan.FromMinutes(5))
                    {
                        HttpContext.Session.Remove("OTP");
                        HttpContext.Session.Remove("AuthUserEmail");
                        HttpContext.Session.Remove("OTPTimestamp");
                        ModelState.AddModelError("", "OTP has expired. Please login again to receive a new code.");
                        _logger.LogWarning("Expired OTP attempt for {Email}", email);
                        return Page();
                    }
                }
            }

            // Validate OTP
            if (OTPInput != sessionOTP)
            {
                ModelState.AddModelError("", "Invalid OTP. Please try again.");
                _logger.LogWarning("Invalid OTP attempt for {Email}", email);
                return Page();
            }

            // OTP is valid - complete login
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // Clear OTP from session
                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove("OTPTimestamp");

                // Update Session & Login tracking
                user.LastLoginDate = DateTime.UtcNow;
                user.LastLoginIP = HttpContext.Connection.RemoteIpAddress?.ToString();
                user.CurrentSessionId = HttpContext.Session.Id;
                await userManager.UpdateAsync(user);

                // Create Claims for MyCookieAuth
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email!),
                    new Claim(ClaimTypes.Email, user.Email!),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim("Department", "HR"),
                    new Claim("LoginTime", DateTime.UtcNow.ToString("o")),
                    new Claim("IP", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"),
                    new Claim("2FAVerified", "true")
                };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

                // Set authentication properties for persistent cookie
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync("MyCookieAuth", claimsPrincipal, authProperties);

                // Audit log - successful 2FA login
                await auditService.LogLoginAsync(user.Id, user.Email!,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    HttpContext.Request.Headers["User-Agent"].ToString(), true);

                _logger.LogInformation("User {Email} completed 2FA login successfully", email);

                // Clear remaining session data
                HttpContext.Session.Remove("AuthUserEmail");
                HttpContext.Session.Remove("AuthUserRemember");

                return RedirectToPage("Index");
            }

            _logger.LogWarning("User not found after valid OTP for email: {Email}", email);
            return RedirectToPage("/Login");
        }
    }
}