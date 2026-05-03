using trendaura.Models;

namespace trendaura.ViewModels
{
    public class AdminReviewsViewModel
    {
        public List<Review> Reviews { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public int? SelectedProductId { get; set; }
        public int TotalCount { get; set; }
    }
}
