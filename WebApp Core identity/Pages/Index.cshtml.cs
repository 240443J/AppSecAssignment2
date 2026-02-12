using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;

namespace WebApp_Core_identity.Pages
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EncryptionService _encryptionService;
        private readonly AuditService _auditService;

        public ApplicationUser? CurrentUser { get; set; }
        public string? DecryptedCreditCard { get; set; }
        public string? MaskedCreditCard { get; set; }
        public string? FormattedMobileNo { get; set; }

        public IndexModel(
            ILogger<IndexModel> logger,
            UserManager<ApplicationUser> userManager,
            EncryptionService encryptionService,
            AuditService auditService)
        {
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _auditService = auditService;
        }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                CurrentUser = user;

                // Decrypt credit card for display
                if (!string.IsNullOrEmpty(user.CreditCardNo))
                {
                    try
                    {
                        DecryptedCreditCard = _encryptionService.Decrypt(user.CreditCardNo);

                        // Create masked version (show last 4 digits only)
                        if (DecryptedCreditCard != null && DecryptedCreditCard.Length == 16)
                        {
                            MaskedCreditCard = "**** **** **** " + DecryptedCreditCard.Substring(12);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to decrypt credit card for user {UserId}", user.Id);
                        DecryptedCreditCard = "[Decryption Error]";
                        MaskedCreditCard = "**** **** **** ****";
                    }
                }

                // Format mobile number for display
                if (!string.IsNullOrEmpty(user.MobileNo) && user.MobileNo.Length == 8)
                {
                    FormattedMobileNo = $"{user.MobileNo.Substring(0, 4)} {user.MobileNo.Substring(4)}";
                }
                else
                {
                    FormattedMobileNo = user.MobileNo;
                }

                // === AUDIT LOG: Profile View ===
                await _auditService.LogProfileViewAsync(
                    user.Id,
                    user.Email!,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    HttpContext.Request.Headers["User-Agent"].ToString());

                _logger.LogInformation("User {Email} viewed their profile at {Time}", user.Email, DateTime.UtcNow);
            }
        }
    }
}
