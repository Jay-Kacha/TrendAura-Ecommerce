using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;

namespace trendaura.Controllers
{
    public class AccessoriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AccessoriesController> _logger;

        public AccessoriesController(ApplicationDbContext db, ILogger<AccessoriesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: Accessories
        public async Task<IActionResult> Index(int? categoryId, string? brand, string? searchTerm, string? sortBy)
        {
            try
            {
                var accessories = _db.MobileAccessories
                    .Include(m => m.Category)
                    .Include(m => m.Reviews)
                    .AsQueryable();

                // Filter by category
                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    accessories = accessories.Where(m => m.CategoryId == categoryId.Value);
                }

                // Filter by brand
                if (!string.IsNullOrEmpty(brand))
                {
                    accessories = accessories.Where(m => m.Brand == brand);
                }

                // Search
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    accessories = accessories.Where(m =>
                        m.Name.Contains(searchTerm) ||
                        m.Brand.Contains(searchTerm) ||
                        (m.Description != null && m.Description.Contains(searchTerm)) ||
                        (m.Compatibility != null && m.Compatibility.Contains(searchTerm)));
                }

                // Sorting
                accessories = sortBy switch
                {
                    "price_asc" => accessories.OrderBy(m => m.Price),
                    "price_desc" => accessories.OrderByDescending(m => m.Price),
                    "name" => accessories.OrderBy(m => m.Name),
                    "rating" => accessories.OrderByDescending(m => m.Reviews != null && m.Reviews.Any() 
                        ? m.Reviews.Average(r => r.Rating) : 0),
                    _ => accessories.OrderByDescending(m => m.CreatedAt)
                };

                // Get distinct brands for filter
                ViewBag.Brands = await _db.MobileAccessories
                    .Select(m => m.Brand)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToListAsync();

                ViewBag.Categories = await _db.Categories.ToListAsync();
                ViewBag.CurrentCategory = categoryId;
                ViewBag.CurrentBrand = brand;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.SortBy = sortBy;

                return View(await accessories.ToListAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading accessories");
                TempData["error"] = "Error loading accessories. Please try again.";
                return View(new List<MobileAccessory>());
            }
        }

        // GET: Accessories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var accessory = await _db.MobileAccessories
                    .Include(m => m.Category)
                    .Include(m => m.Reviews)
                        .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (accessory == null)
                {
                    return NotFound();
                }

                return View(accessory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading accessory details");
                TempData["error"] = "Error loading accessory details.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
