using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;
using System.Security.Claims;

namespace trendaura.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WishlistController> _logger;

        public WishlistController(ApplicationDbContext db, ILogger<WishlistController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var wishlistItems = await _db.Wishlists
                    .Include(w => w.Product)
                    .Include(w => w.MobileAccessory)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.AddedAt)
                    .ToListAsync();

                return View(wishlistItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading wishlist");
                TempData["error"] = "Error loading wishlist. Please try again.";
                return View(new List<Wishlist>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Please login to add items to wishlist.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Check if product exists
                var product = await _db.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                // Check if already in wishlist
                var existing = await _db.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (existing != null)
                {
                    TempData["info"] = "Product is already in your wishlist.";
                    return RedirectToAction("Index");
                }

                var wishlistItem = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow
                };

                _db.Wishlists.Add(wishlistItem);
                await _db.SaveChangesAsync();
                TempData["success"] = "Product added to wishlist!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to wishlist");
                TempData["error"] = "Error adding product to wishlist. Please try again.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccessory(int accessoryId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Please login to add items to wishlist.";
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

                // Check if already in wishlist
                var existing = await _db.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.MobileAccessoryId == accessoryId);

                if (existing != null)
                {
                    TempData["info"] = "Accessory is already in your wishlist.";
                    return RedirectToAction("Index");
                }

                var wishlistItem = new Wishlist
                {
                    UserId = userId,
                    MobileAccessoryId = accessoryId,
                    AddedAt = DateTime.UtcNow
                };

                _db.Wishlists.Add(wishlistItem);
                await _db.SaveChangesAsync();
                TempData["success"] = "Accessory added to wishlist!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding accessory to wishlist");
                TempData["error"] = "Error adding accessory to wishlist. Please try again.";
            }

            return RedirectToAction("Index");
        }

        // Legacy method for backward compatibility
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            return await AddProduct(productId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var item = await _db.Wishlists
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

                if (item != null)
                {
                    _db.Wishlists.Remove(item);
                    await _db.SaveChangesAsync();
                    TempData["success"] = "Item removed from wishlist!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from wishlist");
                TempData["error"] = "Error removing item. Please try again.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var wishlistItem = await _db.Wishlists
                    .Include(w => w.Product)
                    .Include(w => w.MobileAccessory)
                    .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

                if (wishlistItem != null)
                {
                    // Check stock
                    int stock = wishlistItem.Product?.Stock ?? wishlistItem.MobileAccessory?.Stock ?? 0;
                    if (stock < 1)
                    {
                        TempData["error"] = "Item is out of stock.";
                        return RedirectToAction("Index");
                    }

                    // Add to cart
                    CartItem? existingCartItem = null;
                    if (wishlistItem.ProductId.HasValue)
                    {
                        existingCartItem = await _db.CartItems
                            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == wishlistItem.ProductId);

                        if (existingCartItem != null)
                        {
                            existingCartItem.Quantity += 1;
                            _db.CartItems.Update(existingCartItem);
                        }
                        else
                        {
                            _db.CartItems.Add(new CartItem
                            {
                                UserId = userId,
                                ProductId = wishlistItem.ProductId,
                                Quantity = 1
                            });
                        }
                    }
                    else if (wishlistItem.MobileAccessoryId.HasValue)
                    {
                        existingCartItem = await _db.CartItems
                            .FirstOrDefaultAsync(c => c.UserId == userId && c.MobileAccessoryId == wishlistItem.MobileAccessoryId);

                        if (existingCartItem != null)
                        {
                            existingCartItem.Quantity += 1;
                            _db.CartItems.Update(existingCartItem);
                        }
                        else
                        {
                            _db.CartItems.Add(new CartItem
                            {
                                UserId = userId,
                                MobileAccessoryId = wishlistItem.MobileAccessoryId,
                                Quantity = 1
                            });
                        }
                    }

                    // Remove from wishlist
                    _db.Wishlists.Remove(wishlistItem);
                    await _db.SaveChangesAsync();

                    TempData["success"] = "Item moved to cart!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving item to cart");
                TempData["error"] = "Error moving item to cart. Please try again.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var wishlistItems = await _db.Wishlists
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                _db.Wishlists.RemoveRange(wishlistItems);
                await _db.SaveChangesAsync();
                TempData["success"] = "Wishlist cleared!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing wishlist");
                TempData["error"] = "Error clearing wishlist. Please try again.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { count = 0 });
            }

            var count = await _db.Wishlists
                .Where(w => w.UserId == userId)
                .CountAsync();

            return Json(new { count });
        }
    }
}
