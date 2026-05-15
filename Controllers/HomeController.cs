using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using trendaura.Data;
using trendaura.Models;
using trendaura.ViewModels; 

namespace trendaura.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Featured products loading logic - you can customize this as needed
            var featuredProducts = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToListAsync();

            ViewBag.Categories = await _db.Categories.ToListAsync();

            return View(featuredProducts);
        }

        public IActionResult About()
        {
            return View();
        }

        public async Task<IActionResult> Category(int id)
        {
            var products = await _db.Products
                .Where(p => p.CategoryId == id)
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .ToListAsync();

            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Search(string q)
        {
            var products = await _db.Products
                .Where(p => p.Name.Contains(q))
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .ToListAsync();

            ViewBag.Query = q;
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