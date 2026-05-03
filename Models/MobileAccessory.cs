using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trendaura.Models
{
    public class MobileAccessory
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Accessory name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        [Display(Name = "Accessory Name")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Brand is required")]
        [StringLength(100, ErrorMessage = "Brand cannot exceed 100 characters")]
        [Display(Name = "Brand")]
        public string Brand { get; set; } = null!;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Price (Rs)")]
        public decimal Price { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, 99999, ErrorMessage = "Stock must be between 0 and 99,999")]
        [Display(Name = "Stock Quantity")]
        public int Stock { get; set; }

        [StringLength(50)]
        [Display(Name = "Color")]
        public string? Color { get; set; }

        [StringLength(200)]
        [Display(Name = "Compatibility")]
        public string? Compatibility { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Display(Name = "Featured")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedAt { get; set; }

        // Reviews
        public ICollection<AccessoryReview>? Reviews { get; set; }

        // Helper properties
        [NotMapped]
        public double AverageRating => Reviews != null && Reviews.Any()
            ? Math.Round(Reviews.Average(r => r.Rating), 1)
            : 0;

        [NotMapped]
        public int ReviewCount => Reviews?.Count ?? 0;
    }
}
