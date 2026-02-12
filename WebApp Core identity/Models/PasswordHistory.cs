using System.ComponentModel.DataAnnotations;

namespace WebApp_Core_Identity.Model
{
    public class PasswordHistory
    {
[Key]
   public int Id { get; set; }

      [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
 public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
