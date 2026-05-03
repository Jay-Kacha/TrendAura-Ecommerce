using trendaura.Models;

namespace trendaura.ViewModels
{
    public class ProductReviewsViewModel
    {
        public Product Product { get; set; } = null!;
        public List<AccessoryReview> Reviews { get; set; } = new();
        public AccessoryReview NewReview { get; set; } = new();
        public double AverageRating { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
        public bool UserHasReviewed { get; set; }
        public bool UserHasOrdered { get; set; }
    }
}
