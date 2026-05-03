using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;
using trendaura.ViewModels; 
namespace trendaura.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _db.Products.Include(p => p.Category).ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

          
            var viewModel = new ProductReviewsViewModel
            {
                Product = product,
                Reviews = product.Reviews?.ToList() ?? new List<AccessoryReview>(),
                NewReview = new AccessoryReview { AccessoryId = id }
            };

            return View(viewModel);
        }
    }
}