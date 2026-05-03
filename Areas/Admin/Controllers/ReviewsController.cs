using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;

namespace trendaura.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReviewsController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(int? productId)
        {
            var reviewsQuery = _db.Reviews.Include(r => r.Product).Include(r => r.User).AsQueryable();
            if (productId.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.ProductId == productId.Value);
            }

            var model = new trendaura.ViewModels.AdminReviewsViewModel
            {
                Reviews = await reviewsQuery.ToListAsync(),
                Products = await _db.Products.ToListAsync(),
                SelectedProductId = productId,
                TotalCount = await _db.Reviews.CountAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.Reviews.FindAsync(id);
            if (r != null)
            {
                _db.Reviews.Remove(r);
                await _db.SaveChangesAsync();
                TempData["success"] = "Review deleted.";
            }
            return RedirectToAction("Index");
        }
    }
}
