using System.ComponentModel.DataAnnotations;

namespace trendaura.ViewModels
{
    public class CheckoutViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string CustomerName { get; set; } = null!;

        [Required]
        [Display(Name = "Phone Number")]
        [Phone]
        [StringLength(20)]
        public string CustomerPhone { get; set; } = null!;

        [Required]
        [Display(Name = "Shipping Address")]
        [StringLength(500)]
        public string ShippingAddress { get; set; } = null!;

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash on Delivery";

        // Online Payment Fields (optional, only when PaymentMethod is online)
        [Display(Name = "Card Number")]
        [StringLength(16)]
        public string? CardNumber { get; set; }

        [Display(Name = "Card Holder Name")]
        [StringLength(100)]
        public string? CardHolderName { get; set; }

        [Display(Name = "Expiry Month")]
        [Range(1, 12)]
        public int? ExpiryMonth { get; set; }

        [Display(Name = "Expiry Year")]
        [Range(2024, 2050)]
        public int? ExpiryYear { get; set; }

        [Display(Name = "CVV")]
        [StringLength(4)]
        public string? CVV { get; set; }
    }
}
