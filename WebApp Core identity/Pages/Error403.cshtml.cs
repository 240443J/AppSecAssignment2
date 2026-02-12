using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp_Core_identity.Pages
{
    public class Error403Model : PageModel
    {
        private readonly ILogger<Error403Model> _logger;

   public Error403Model(ILogger<Error403Model> logger)
        {
_logger = logger;
      }

  public void OnGet()
  {
     var sanitizedPath = HttpContext.Request.Path.ToString().Replace("\r", string.Empty).Replace("\n", string.Empty);
     _logger.LogWarning("403 Error: Access denied - Path: {Path}, User: {User}, IP: {IP}", 
      sanitizedPath,
     User.Identity?.Name ?? "Anonymous",
     HttpContext.Connection.RemoteIpAddress);
        }
    }
}
