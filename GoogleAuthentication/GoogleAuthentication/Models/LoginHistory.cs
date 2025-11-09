using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoogleAuthentication.Models
{
    [Table("LoginHistory")]
    public class LoginHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LoginHistoryId { get; set; }
        [Required]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        public string Provider { get; set; } = string.Empty;

        [Required]
        public string ActionType { get; set; } = string.Empty; // "Login" or "Logout"

        public string? IpAddress { get; set; }

        public DateTime ActionTime { get; set; } = DateTime.UtcNow;
    }
}
