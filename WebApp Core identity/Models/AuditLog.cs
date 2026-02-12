using System.ComponentModel.DataAnnotations;

namespace WebApp_Core_Identity.Model
{
    public class AuditLog
    {
   [Key]
        public int Id { get; set; }

 [Required]
   public string UserId { get; set; } = string.Empty;

  public string? UserEmail { get; set; }

  [Required]
   public string Action { get; set; } = string.Empty;

      public string? Details { get; set; }

 [Required]
 public string IpAddress { get; set; } = string.Empty;

        public string? UserAgent { get; set; }

   [Required]
   public DateTime Timestamp { get; set; } = DateTime.UtcNow;

   public string? Result { get; set; }

     public string? ErrorMessage { get; set; }
    }
}
