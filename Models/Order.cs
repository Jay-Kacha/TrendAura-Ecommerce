using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trendaura.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash on Delivery";
        
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending";
        
        [StringLength(200)]
        public string? TransactionId { get; set; }
        
        [StringLength(500)]
        public string? ShippingAddress { get; set; }
        
        [StringLength(100)]
        public string? CustomerName { get; set; }
        
        [StringLength(20)]
        public string? CustomerPhone { get; set; }

        public ApplicationUser? User { get; set; }
        public List<OrderItem>? Items { get; set; }
    }
}
