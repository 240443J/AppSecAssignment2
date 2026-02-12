using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;
using WebApp_Core_Identity.Helpers;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace WebApp_Core_identity.Pages
{
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

        public async Task<IActionResult> OnPostAsync(string? captchaToken)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // === SECURITY: Google reCAPTCHA v3 verification ===
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
                        // Bot detected - reject request
                        ModelState.AddModelError("", "Request failed. Please try again.");
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                        var score = captchaResult?.score?.ToString() ?? "unknown";
                        _logger.LogWarning($"reCAPTCHA failed for password reset attempt from IP: {ipAddress}, Score: {score}");

                        // Log security event
                        await _auditService.LogSecurityEventAsync(
                            "Anonymous",
                            "Bot Password Reset Attempt",
                            $"reCAPTCHA score: {score}",
                            ipAddress,
                            HttpContext.Request.Headers["User-Agent"].ToString());

                        return Page();
                    }

                    var passIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                    var passScore = captchaResult?.score?.ToString() ?? "unknown";
                    _logger.LogInformation($"reCAPTCHA passed for password reset from IP: {passIpAddress}, Score: {passScore}");
                }

                // Validate email format
                if (!InputValidationHelper.IsValidEmail(Email))
                {
                    ModelState.AddModelError("Email", "Invalid email format");
                    return Page();
                }

                // Sanitize email
                var sanitizedEmail = InputValidationHelper.SanitizeInput(Email);

                var user = await _userManager.FindByEmailAsync(sanitizedEmail);

                // Always show success message (don't reveal if email exists - security best practice)
                StatusMessage = "If an account with that email exists, a password reset link has been sent.";

                if (user != null)
                {
                    // Generate secure token
                    var token = GenerateSecureToken();
                    user.PasswordResetToken = token;
                    user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

                    await _userManager.UpdateAsync(user);

                    // Generate reset link
                    var resetLink = Url.Page("/ResetPassword", null, new { token, email = sanitizedEmail }, Request.Scheme);

                    // Send email
                    await _emailService.SendPasswordResetEmailAsync(sanitizedEmail, resetLink!);

                    // Audit log
                    await _auditService.LogAsync(
                        user.Id,
                        user.Email,
                        "PasswordResetRequest",
                        "Password reset email sent",
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                        HttpContext.Request.Headers["User-Agent"].ToString());

                    _logger.LogInformation("Password reset requested for {Email}", sanitizedEmail);
                }
                else
                {
                    // Log attempt for non-existent email (security monitoring)
                    await _auditService.LogSecurityEventAsync(
                        "Unknown",
                        "PasswordResetAttempt",
                        $"Password reset attempted for non-existent email: {sanitizedEmail}",
                        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                        HttpContext.Request.Headers["User-Agent"].ToString());

                    _logger.LogWarning("Password reset attempted for non-existent email: {Email}", sanitizedEmail);
                }

                return RedirectToPage("/ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset for {Email}", Email);
                ModelState.AddModelError(string.Empty, "An error occurred while processing your request. Please try again.");
                return Page();
            }
        }

        private string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenData = new byte[32];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
