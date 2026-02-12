using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp_Core_identity.Pages
{
    [Authorize(Policy = "MustBelongToHRDepartment", AuthenticationSchemes = "MyCookieAuth")]
        public class HumanResourceModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
