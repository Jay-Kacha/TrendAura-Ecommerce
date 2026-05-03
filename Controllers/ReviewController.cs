using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;
using trendaura.ViewModels;

namespace trendaura.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ProductReviewsViewModel model)
        {
            var reviewModel = model?.NewReview;
            if (reviewModel == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "You must be logged in to write a review.";
                return RedirectToAction("Login", "Account");
            }

            var existingReview = await _db.AccessoryReviews
                .FirstOrDefaultAsync(r => r.AccessoryId == reviewModel.AccessoryId && r.UserId == userId);

            if (existingReview != null)
            {
                TempData["error"] = "You have already reviewed this product.";
                return RedirectToAction("Details", "Home", new { id = reviewModel.AccessoryId });
            }

            // Check verified purchase
            var hasOrdered = await _db.Orders
                .Include(o => o.Items)
                .AnyAsync(o => o.UserId == userId &&
                               o.Items.Any(i => i.ProductId == reviewModel.AccessoryId));

            var user = await _userManager.GetUserAsync(User);

          
            var review = new AccessoryReview
            {
                AccessoryId = reviewModel.AccessoryId,
                UserId = userId,
                Rating = reviewModel.Rating,
                Comment = reviewModel.Comment,
                ReviewerName = user?.FullName ?? user?.UserName ?? "Anonymous",
                IsVerifiedPurchase = hasOrdered,
                CreatedAt = DateTime.Now
            };

            _db.AccessoryReviews.Add(review);
            await _db.SaveChangesAsync();

            TempData["success"] = "Thank you for your review!";
            return RedirectToAction("Details", "Home", new { id = reviewModel.AccessoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "You must be logged in to delete a review.";
                return RedirectToAction("Login", "Account");
            }

            var review = await _db.AccessoryReviews.FindAsync(id);

            if (review == null)
            {
                TempData["error"] = "Review not found.";
                return RedirectToAction("Details", "Home", new { id = productId });
            }

            if (review.UserId != userId)
            {
                TempData["error"] = "You can only delete your own reviews.";
                return RedirectToAction("Details", "Home", new { id = productId });
            }

            _db.AccessoryReviews.Remove(review);
            await _db.SaveChangesAsync();

            TempData["success"] = "Your review has been deleted.";
            return RedirectToAction("Details", "Home", new { id = productId });
        }
    }
}