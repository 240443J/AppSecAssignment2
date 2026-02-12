using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;

namespace WebApp_Core_identity.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly AuditService auditService;

        public LogoutModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            AuditService auditService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.auditService = auditService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            var user = await userManager.GetUserAsync(User);

            await signInManager.SignOutAsync();

            if (user != null)
            {
                // === AUDIT LOG: Logout ===
                await auditService.LogLogoutAsync(
                    user.Id,
                    user.Email!,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    HttpContext.Request.Headers["User-Agent"].ToString());
            }

            return RedirectToPage("Login");
        }

        public async Task<IActionResult> OnPostDontLogoutAsync()
        {
            return RedirectToPage("Index");
        }
    }
}
