using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace trendaura.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string? ImageUrl { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int Stock { get; set; } = 100;

        [ForeignKey("AccessoryId")]
        public virtual ICollection<AccessoryReview> Reviews { get; set; } = new List<AccessoryReview>();

        [NotMapped]
        public double AverageRating => Reviews != null && Reviews.Any()
            ? Math.Round(Reviews.Average(r => r.Rating), 1)
            : 0;

        [NotMapped]
        public int ReviewCount => Reviews?.Count ?? 0;
    }
}