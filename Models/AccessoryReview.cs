using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trendaura.Models
{
    public class AccessoryReview
    {
        [Key]
        public int Id { get; set; }

        public int AccessoryId { get; set; }
        public MobileAccessory? Accessory { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? ReviewerName { get; set; }
        public bool IsVerifiedPurchase { get; set; }
    }
}