using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WebApp_Core_Identity.Configuration;
using WebApp_Core_Identity.Model;

namespace WebApp_Core_Identity.Services
{
  public interface IPasswordAgeService
    {
        /// <summary>
        /// Checks if user can change password (minimum age check)
        /// </summary>
        Task<(bool CanChange, string? ErrorMessage, TimeSpan? TimeRemaining)> CanChangePasswordAsync(ApplicationUser user);

        /// <summary>
        /// Checks if password has expired (maximum age check)
        /// </summary>
      Task<(bool IsExpired, int DaysUntilExpiry)> IsPasswordExpiredAsync(ApplicationUser user);

    /// <summary>
        /// Updates password last changed date
        /// </summary>
   Task UpdatePasswordChangeDateAsync(ApplicationUser user);

     /// <summary>
   /// Checks if user should see password expiry warning
   /// </summary>
        Task<(bool ShowWarning, int DaysRemaining)> ShouldShowExpiryWarningAsync(ApplicationUser user);

  /// <summary>
  /// Marks user to change password on next login
        /// </summary>
        Task SetMustChangePasswordAsync(ApplicationUser user, bool mustChange);

        /// <summary>
        /// Gets time remaining until password expires in seconds (for short durations)
    /// </summary>
        Task<double> GetSecondsUntilExpiryAsync(ApplicationUser user);
  }

    public class PasswordAgeService : IPasswordAgeService
    {
 private readonly UserManager<ApplicationUser> _userManager;
      private readonly PasswordAgeSettings _settings;
     private readonly ILogger<PasswordAgeService> _logger;

        public PasswordAgeService(
      UserManager<ApplicationUser> userManager,
      IOptions<PasswordAgeSettings> settings,
      ILogger<PasswordAgeService> logger)
   {
            _userManager = userManager;
          _settings = settings.Value;
   _logger = logger;
  }

public async Task<(bool CanChange, string? ErrorMessage, TimeSpan? TimeRemaining)> CanChangePasswordAsync(ApplicationUser user)
 {
   if (!_settings.EnforceMinimumPasswordAge)
      {
      return (true, null, null);
  }

     if (!user.PasswordLastChangedDate.HasValue)
            {
     // First time changing password - allow it
       return (true, null, null);
      }

  var timeSinceLastChange = DateTime.UtcNow - user.PasswordLastChangedDate.Value;
     var minimumAge = TimeSpan.FromMinutes(_settings.MinimumPasswordAgeMinutes);

            if (timeSinceLastChange < minimumAge)
{
        var timeRemaining = minimumAge - timeSinceLastChange;
             var errorMessage = _settings.MinimumPasswordAgeMinutes < 60
 ? $"You can only change your password every {_settings.MinimumPasswordAgeMinutes} minute(s). Please wait {Math.Ceiling(timeRemaining.TotalMinutes)} more minute(s)."
       : $"You can only change your password every {_settings.MinimumPasswordAgeMinutes / 60} hour(s). Please wait {Math.Ceiling(timeRemaining.TotalHours)} more hour(s).";

          _logger.LogWarning("User {Email} attempted to change password before minimum age. Time remaining: {TimeRemaining}", 
             user.Email, timeRemaining);

          return (false, errorMessage, timeRemaining);
 }

 return (true, null, null);
        }

        public async Task<(bool IsExpired, int DaysUntilExpiry)> IsPasswordExpiredAsync(ApplicationUser user)
  {
 if (!_settings.EnforcePasswordExpiry)
{
   return (false, int.MaxValue);
      }

   if (!user.PasswordLastChangedDate.HasValue)
            {
     // No password change date - assume password was set at account creation
// Give them the full maximum age
        return (false, (int)Math.Ceiling(_settings.MaximumPasswordAgeDays));
      }

            var passwordAge = DateTime.UtcNow - user.PasswordLastChangedDate.Value;
         var maxAge = TimeSpan.FromDays(_settings.MaximumPasswordAgeDays);
   var daysUntilExpiry = (int)Math.Ceiling((maxAge - passwordAge).TotalDays);

   if (passwordAge >= maxAge)
   {
       _logger.LogWarning("Password expired for user {Email}. Age: {Age} days", 
          user.Email, passwordAge.TotalDays);
   return (true, 0);
      }

       return (false, daysUntilExpiry);
        }

        public async Task UpdatePasswordChangeDateAsync(ApplicationUser user)
  {
            user.PasswordLastChangedDate = DateTime.UtcNow;
user.MustChangePassword = false; // Reset flag when password is changed
  
            var result = await _userManager.UpdateAsync(user);
            
 if (result.Succeeded)
            {
      _logger.LogInformation("Updated password change date for user {Email}", user.Email);
}
      else
     {
_logger.LogError("Failed to update password change date for user {Email}", user.Email);
      }
  }

        public async Task<(bool ShowWarning, int DaysRemaining)> ShouldShowExpiryWarningAsync(ApplicationUser user)
     {
            if (!_settings.EnforcePasswordExpiry)
   {
         return (false, int.MaxValue);
   }

var (isExpired, daysUntilExpiry) = await IsPasswordExpiredAsync(user);

  if (isExpired)
     {
         return (true, 0);
    }

       // For very short expiry times (less than 1 day), always show warning
   if (_settings.MaximumPasswordAgeDays < 1)
  {
        return (true, daysUntilExpiry);
   }

   if (daysUntilExpiry <= _settings.PasswordExpiryWarningDays)
 {
    return (true, daysUntilExpiry);
       }

  return (false, daysUntilExpiry);
        }

        /// <summary>
    /// Gets time remaining until password expires in seconds (for short durations)
    /// </summary>
        public async Task<double> GetSecondsUntilExpiryAsync(ApplicationUser user)
        {
            if (!_settings.EnforcePasswordExpiry || !user.PasswordLastChangedDate.HasValue)
            {
    return double.MaxValue;
  }

     var passwordAge = DateTime.UtcNow - user.PasswordLastChangedDate.Value;
            var maxAge = TimeSpan.FromDays(_settings.MaximumPasswordAgeDays);
  var timeRemaining = maxAge - passwordAge;

            return timeRemaining.TotalSeconds;
        }

        public async Task SetMustChangePasswordAsync(ApplicationUser user, bool mustChange)
        {
     user.MustChangePassword = mustChange;
      
   if (mustChange)
  {
       _logger.LogWarning("User {Email} marked to change password on next login", user.Email);
  }

      await _userManager.UpdateAsync(user);
  }
    }
}
