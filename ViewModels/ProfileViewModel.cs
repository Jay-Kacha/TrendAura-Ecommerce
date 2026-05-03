using System.ComponentModel.DataAnnotations;

namespace trendaura.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Display(Name = "Phone Number")]
        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Street Address")]
        [StringLength(200)]
        public string? Address { get; set; }

        [Display(Name = "City")]
        [StringLength(100)]
        public string? City { get; set; }

        [Display(Name = "State / Province")]
        [StringLength(100)]
        public string? State { get; set; }

        [Display(Name = "Zip / Postal Code")]
        [StringLength(20)]
        public string? ZipCode { get; set; }

        [Display(Name = "Country")]
        [StringLength(100)]
        public string? Country { get; set; }
    }
}
