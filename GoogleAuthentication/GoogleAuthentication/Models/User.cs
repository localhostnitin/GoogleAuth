using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoogleAuthentication.Models
{
    [Table("Users")]
    public class ApplicationUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(150)]
        [EmailAddress]
        public string Email { get; set; }
        public string ProviderKey { get; set; } = default!; // provider user id
        [Required, StringLength(50)]
        public string Provider { get; set; } // e.g., Google, AzureAD, Facebook
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime LoginTime{ get; set; }
    }
}
