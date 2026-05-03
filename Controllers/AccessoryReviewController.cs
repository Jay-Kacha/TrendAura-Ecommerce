using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;
using System.Security.Claims;

namespace trendaura.Controllers
{
    [Authorize]
    public class AccessoryReviewController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AccessoryReviewController> _logger;

        public AccessoryReviewController(ApplicationDbContext db, ILogger<AccessoryReviewController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int accessoryId, int rating, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Please login to add a review.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Check if accessory exists
                var accessory = await _db.MobileAccessories.FindAsync(accessoryId);
                if (accessory == null)
                {
                    TempData["error"] = "Accessory not found.";
                    return RedirectToAction("Index", "Accessories");
                }

                // Check if user already reviewed this accessory
                var existingReview = await _db.AccessoryReviews
                    .FirstOrDefaultAsync(r => r.AccessoryId == accessoryId && r.UserId == userId);

                if (existingReview != null)
                {
                    TempData["error"] = "You have already reviewed this accessory.";
                    return RedirectToAction("Details", "Accessories", new { id = accessoryId });
                }

                // Get user details
                var user = await _db.Users.FindAsync(userId);

                // Check if user has purchased this accessory
                bool isVerifiedPurchase = await _db.Orders
                    .Where(o => o.UserId == userId && o.Status == "Delivered")
                    .AnyAsync();

                var review = new AccessoryReview
                {
                    AccessoryId = accessoryId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    ReviewerName = user?.FullName ?? user?.Email ?? "Anonymous",
                    IsVerifiedPurchase = isVerifiedPurchase,
                    CreatedAt = DateTime.UtcNow
                };

                _db.AccessoryReviews.Add(review);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Review added for accessory {accessoryId} by user {userId}");
                TempData["success"] = "Review added successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding review");
                TempData["error"] = "Error adding review. Please try again.";
            }

            return RedirectToAction("Details", "Accessories", new { id = accessoryId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int reviewId, int rating, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var review = await _db.AccessoryReviews
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

                if (review == null)
                {
                    TempData["error"] = "Review not found or you don't have permission to edit it.";
                    return RedirectToAction("Index", "Accessories");
                }

                review.Rating = rating;
                review.Comment = comment;

                _db.AccessoryReviews.Update(review);
                await _db.SaveChangesAsync();

                TempData["success"] = "Review updated successfully!";
                return RedirectToAction("Details", "Accessories", new { id = review.AccessoryId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review");
                TempData["error"] = "Error updating review. Please try again.";
                return RedirectToAction("Index", "Accessories");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int reviewId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var review = await _db.AccessoryReviews
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

                if (review == null)
                {
                    TempData["error"] = "Review not found or you don't have permission to delete it.";
                    return RedirectToAction("Index", "Accessories");
                }

                var accessoryId = review.AccessoryId;

                _db.AccessoryReviews.Remove(review);
                await _db.SaveChangesAsync();

                TempData["success"] = "Review deleted successfully!";
                return RedirectToAction("Details", "Accessories", new { id = accessoryId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review");
                TempData["error"] = "Error deleting review. Please try again.";
                return RedirectToAction("Index", "Accessories");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviews(int accessoryId, int page = 1, int pageSize = 5)
        {
            try
            {
                var reviews = await _db.AccessoryReviews
                    .Include(r => r.User)
                    .Where(r => r.AccessoryId == accessoryId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.Id,
                        r.Rating,
                        r.Comment,
                        r.ReviewerName,
                        r.IsVerifiedPurchase,
                        CreatedAt = r.CreatedAt.ToString("MMM dd, yyyy"),
                        IsOwner = r.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)
                    })
                    .ToListAsync();

                var totalReviews = await _db.AccessoryReviews
                    .Where(r => r.AccessoryId == accessoryId)
                    .CountAsync();

                return Json(new
                {
                    reviews,
                    totalReviews,
                    currentPage = page,
                    totalPages = (int)Math.Ceiling(totalReviews / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reviews");
                return Json(new { error = "Error loading reviews" });
            }
        }
    }
}
