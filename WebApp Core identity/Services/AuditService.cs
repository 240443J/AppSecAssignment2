using WebApp_Core_Identity.Model;

namespace WebApp_Core_Identity.Services
{
    public class AuditService
    {
        private readonly AuthDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(AuthDbContext context, ILogger<AuditService> logger)
   {
   _context = context;
  _logger = logger;
        }

        public async Task LogAsync(
   string userId,
   string? userEmail,
   string action,
      string? details,
  string ipAddress,
   string? userAgent,
     string? result = "Success",
string? errorMessage = null)
        {
   try
   {
  var auditLog = new AuditLog
    {
  UserId = userId,
   UserEmail = userEmail,
   Action = action,
     Details = details,
    IpAddress = ipAddress,
      UserAgent = userAgent,
       Timestamp = DateTime.UtcNow,
     Result = result,
ErrorMessage = errorMessage
   };

     _context.AuditLogs.Add(auditLog);
     await _context.SaveChangesAsync();

  _logger.LogInformation("Audit log created: {Action} by {User} from {IP}", 
         action, userEmail ?? userId, ipAddress);
    }
   catch (Exception ex)
   {
    _logger.LogError(ex, "Failed to create audit log for action: {Action}", action);
     }
  }

   public async Task LogLoginAsync(string userId, string userEmail, string ipAddress, string? userAgent, bool success, string? errorMessage = null)
        {
    await LogAsync(
        userId,
     userEmail,
    "Login",
   $"User attempted login",
ipAddress,
    userAgent,
       success ? "Success" : "Failed",
    errorMessage
   );
        }

public async Task LogLogoutAsync(string userId, string userEmail, string ipAddress, string? userAgent)
 {
       await LogAsync(
   userId,
     userEmail,
     "Logout",
   "User logged out",
     ipAddress,
    userAgent
   );
}

  public async Task LogRegistrationAsync(string userId, string userEmail, string ipAddress, string? userAgent)
  {
 await LogAsync(
    userId,
      userEmail,
   "Registration",
   "New user registered",
       ipAddress,
      userAgent
   );
 }

        public async Task LogPasswordChangeAsync(string userId, string userEmail, string ipAddress, string? userAgent)
 {
   await LogAsync(
      userId,
   userEmail,
     "PasswordChange",
 "User changed password",
ipAddress,
   userAgent
      );
  }

 public async Task LogProfileViewAsync(string userId, string userEmail, string ipAddress, string? userAgent)
 {
   await LogAsync(
     userId,
      userEmail,
 "ProfileView",
      "User viewed profile",
   ipAddress,
      userAgent
    );
        }

  public async Task LogSecurityEventAsync(string userId, string action, string details, string ipAddress, string? userAgent)
      {
    await LogAsync(
     userId,
    null,
   action,
   details,
 ipAddress,
    userAgent,
   "SecurityEvent"
            );
   }
    }
}
