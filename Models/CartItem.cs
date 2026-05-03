using System.ComponentModel.DataAnnotations.Schema;

namespace trendaura.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        
        // For Products
        public int? ProductId { get; set; }
        public Product? Product { get; set; }
        
        // For Mobile Accessories
        public int? MobileAccessoryId { get; set; }
        public MobileAccessory? MobileAccessory { get; set; }
        
        public int Quantity { get; set; }
        
        // Helper properties
        [NotMapped]
        public string ItemName => Product?.Name ?? MobileAccessory?.Name ?? "Unknown";
        
        [NotMapped]
        public decimal ItemPrice => Product?.Price ?? MobileAccessory?.Price ?? 0;
        
        [NotMapped]
        public string? ItemImageUrl => Product?.ImageUrl ?? MobileAccessory?.ImageUrl;
        
        [NotMapped]
        public int ItemStock => Product?.Stock ?? MobileAccessory?.Stock ?? 0;
        
        [NotMapped]
        public decimal TotalPrice => ItemPrice * Quantity;
    }
}
