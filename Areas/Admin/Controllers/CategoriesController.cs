using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using trendaura.Data;
using trendaura.Models;

namespace trendaura.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _db.Categories.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (!ModelState.IsValid) return View(category);
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            TempData["success"] = "Category created.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var cat = await _db.Categories.FindAsync(id.Value);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (!ModelState.IsValid) return View(category);
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();
            TempData["success"] = "Category updated.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return RedirectToAction("Index");

            var cat = await _db.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id.Value);

            if (cat != null)
            {
                try
                {
                    var productIds = cat.Products?.Select(p => p.Id).ToList() ?? new List<int>();

                    if (productIds.Any())
                    {
                        // 1. Delete CartItems
                        var cartItemsToDelete = await _db.CartItems
                            .Where(ci => ci.ProductId.HasValue && productIds.Contains(ci.ProductId.Value))
                            .ToListAsync();
                        _db.CartItems.RemoveRange(cartItemsToDelete);

                        // 2. Delete Wishlist items
                        var wishlistItemsToDelete = await _db.Wishlists
                            .Where(w => w.ProductId.HasValue && productIds.Contains(w.ProductId.Value))
                            .ToListAsync();
                        _db.Wishlists.RemoveRange(wishlistItemsToDelete);

                        // 3. Delete AccessoryReviews (Table name updated to AccessoryReviews)
                        var reviewsToDelete = await _db.AccessoryReviews
                            .Where(r => productIds.Contains(r.AccessoryId))
                            .ToListAsync();
                        _db.AccessoryReviews.RemoveRange(reviewsToDelete);
                    }

                    _db.Categories.Remove(cat);
                    await _db.SaveChangesAsync();
                    TempData["success"] = "Category deleted successfully.";
                }
                catch (Exception ex)
                {
                    TempData["error"] = $"Error: {ex.Message}";
                }
            }
            return RedirectToAction("Index");
        }
    }
}