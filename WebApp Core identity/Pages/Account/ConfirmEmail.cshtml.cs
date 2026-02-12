using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp_Core_Identity.Model;

namespace WebApp_Core_identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        // Inject UserManager so we can update the database
        public ConfirmEmailModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            // 1. Basic Validation
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            // 2. Find the user in the MSSQL Database
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            // 3. SECURE VERIFICATION: Validate the token and flip the bit
            // This is the core logic that changes EmailConfirmed from 0 to 1
            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                // Return to the page to show a "Success" message to the user
                return Page();
            }
            else
            {
                return Content("Error confirming your email. The token may be expired or invalid.");
            }
        }
    }
}