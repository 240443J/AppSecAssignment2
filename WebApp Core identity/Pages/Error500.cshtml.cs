using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp_Core_identity.Pages
{
    public class Error500Model : PageModel
 {
 private readonly ILogger<Error500Model> _logger;
   private readonly IWebHostEnvironment _environment;

public string ErrorId { get; set; } = Guid.NewGuid().ToString();
   public string? ErrorMessage { get; set; }
        public bool ShowDetails { get; set; }

        public Error500Model(ILogger<Error500Model> logger, IWebHostEnvironment environment)
 {
       _logger = logger;
        _environment = environment;
        }

        public void OnGet(string? message = null)
        {
  ShowDetails = _environment.IsDevelopment();
        ErrorMessage = message;

            var sanitizedPath = HttpContext.Request.Path.ToString().Replace("\r", "").Replace("\n", "");

   _logger.LogError("500 Error: Internal server error - ErrorId: {ErrorId}, Path: {Path}, IP: {IP}, Message: {Message}", 
       ErrorId,
            sanitizedPath,
    HttpContext.Connection.RemoteIpAddress,
       message ?? "No details available");
        }
    }
}
