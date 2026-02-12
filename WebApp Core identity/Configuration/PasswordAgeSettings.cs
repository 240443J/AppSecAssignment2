namespace WebApp_Core_Identity.Configuration
{
    /// <summary>
    /// Configuration settings for password age policies
    /// </summary>
    public class PasswordAgeSettings
    {
  /// <summary>
      /// Minimum time (in minutes) that must pass before a user can change their password again
        /// Default: 1 minute (for testing), Production: 1440 minutes (24 hours)
   /// </summary>
      public int MinimumPasswordAgeMinutes { get; set; } = 1;

  /// <summary>
    /// Maximum time (in days) before a password expires and must be changed
   /// Default: 90 days
        /// Can be fractional for testing (e.g., 0.000694 = 1 minute)
  /// </summary>
public double MaximumPasswordAgeDays { get; set; } = 90;

  /// <summary>
     /// Number of days before password expiry to start showing warnings
/// Default: 14 days
        /// </summary>
      public int PasswordExpiryWarningDays { get; set; } = 14;

/// <summary>
/// Whether to enforce password expiry (force password change)
        /// Default: true
      /// </summary>
   public bool EnforcePasswordExpiry { get; set; } = true;

  /// <summary>
        /// Whether to enforce minimum password age
        /// Default: true
        /// </summary>
 public bool EnforceMinimumPasswordAge { get; set; } = true;
    }
}
