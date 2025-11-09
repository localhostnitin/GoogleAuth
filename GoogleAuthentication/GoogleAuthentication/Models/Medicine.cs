using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoogleAuthentication.Models
{
    [Table("Medicines")]
    public class Medicine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Medicine name is required.")]
        [StringLength(100)]
        [Display(Name = "Medicine name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(100)]
        public string Company { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "Expiry Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name ="Expiry Date")]
        [Range(typeof(DateTime), "2020-01-01", "2100-12-31", ErrorMessage = "Expiry Date must be after the year 2020")]
        public DateTime? ExpiryDate { get; set; }

        [Required(ErrorMessage = "Stock value is required.")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be a non-negative number.")]
        public int? Stock { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
