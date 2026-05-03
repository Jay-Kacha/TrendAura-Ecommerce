using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;
using System.Security.Claims;

namespace trendaura.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext db, ILogger<CartController> logger)
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
                var cartItems = await _db.CartItems
                    .Include(c => c.Product)
                    .Include(c => c.MobileAccessory)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                return View(cartItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["error"] = "Error loading cart. Please try again.";
                return View(new List<CartItem>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(int productId, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Please login to add items to cart.";
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

                // Check stock
                if (product.Stock < quantity)
                {
                    TempData["error"] = "Insufficient stock available.";
                    return RedirectToAction("Details", "Product", new { id = productId });
                }

                var existingItem = await _db.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    _db.CartItems.Update(existingItem);
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = quantity
                    };
                    _db.CartItems.Add(cartItem);
                }

                await _db.SaveChangesAsync();
                TempData["success"] = "Product added to cart!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                TempData["error"] = "Error adding product to cart. Please try again.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccessory(int accessoryId, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Please login to add items to cart.";
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

                // Check stock
                if (accessory.Stock < quantity)
                {
                    TempData["error"] = "Insufficient stock available.";
                    return RedirectToAction("Details", "Accessories", new { id = accessoryId });
                }

                var existingItem = await _db.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.MobileAccessoryId == accessoryId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    _db.CartItems.Update(existingItem);
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        UserId = userId,
                        MobileAccessoryId = accessoryId,
                        Quantity = quantity
                    };
                    _db.CartItems.Add(cartItem);
                }

                await _db.SaveChangesAsync();
                TempData["success"] = "Accessory added to cart!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding accessory to cart");
                TempData["error"] = "Error adding accessory to cart. Please try again.";
            }

            return RedirectToAction("Index");
        }

        // Legacy method for backward compatibility
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            return await AddProduct(productId, quantity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int qty)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var item = await _db.CartItems
                    .Include(c => c.Product)
                    .Include(c => c.MobileAccessory)
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (item != null)
                {
                    if (qty > 0)
                    {
                        // Check stock
                        int availableStock = item.Product?.Stock ?? item.MobileAccessory?.Stock ?? 0;
                        if (qty > availableStock)
                        {
                            TempData["error"] = $"Only {availableStock} items available in stock.";
                            return RedirectToAction("Index");
                        }

                        item.Quantity = qty;
                        _db.CartItems.Update(item);
                        await _db.SaveChangesAsync();
                        TempData["success"] = "Cart updated!";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                TempData["error"] = "Error updating cart. Please try again.";
            }

            return RedirectToAction("Index");
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
                var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
                if (item != null)
                {
                    _db.CartItems.Remove(item);
                    await _db.SaveChangesAsync();
                    TempData["success"] = "Item removed from cart!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                TempData["error"] = "Error removing item. Please try again.";
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
                var cartItems = await _db.CartItems.Where(c => c.UserId == userId).ToListAsync();
                _db.CartItems.RemoveRange(cartItems);
                await _db.SaveChangesAsync();
                TempData["success"] = "Cart cleared!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["error"] = "Error clearing cart. Please try again.";
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

            var count = await _db.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);

            return Json(new { count });
        }
    }
}
