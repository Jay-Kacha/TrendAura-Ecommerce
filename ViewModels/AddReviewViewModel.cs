using System.ComponentModel.DataAnnotations;

namespace trendaura.ViewModels
{
    public class AddReviewViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Please write a review")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Review must be between 10 and 1000 characters")]
        [Display(Name = "Your Review")]
        public string Comment { get; set; } = null!;
    }
}
