using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;
using trendaura.ViewModels;
using System.Security.Claims;

namespace trendaura.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<CheckoutController> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User not authenticated, redirecting to login");
                    return RedirectToAction("Login", "Account");
                }

                var items = await _db.CartItems
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Product)
                    .Include(c => c.MobileAccessory)
                    .ToListAsync();
                
                if (!items.Any())
                {
                    _logger.LogInformation($"Cart empty for user {userId}");
                    TempData["error"] = "Your cart is empty!";
                    return RedirectToAction("Index", "Cart");
                }

                // Pre-fill user information
                var user = await _userManager.GetUserAsync(User);
                var model = new CheckoutViewModel
                {
                    CustomerName = user?.FullName ?? "",
                    CustomerPhone = user?.PhoneNumber ?? "",
                    ShippingAddress = !string.IsNullOrEmpty(user?.Address) 
                        ? $"{user.Address}, {user.City}, {user.State} {user.ZipCode}, {user.Country}"
                        : "",
                    PaymentMethod = "Cash on Delivery"
                };

                ViewBag.CartItems = items;
                ViewBag.Total = items.Sum(i => i.TotalPrice);

                _logger.LogInformation($"Checkout page loaded for user {userId} with {items.Count} items");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                TempData["error"] = "Error loading checkout. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            try
            {
                _logger.LogInformation("PlaceOrder called");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User not authenticated");
                    TempData["error"] = "Please login to place an order.";
                    return RedirectToAction("Login", "Account");
                }

                var items = await _db.CartItems
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Product)
                    .Include(c => c.MobileAccessory)
                    .ToListAsync();
                
                if (!items.Any())
                {
                    _logger.LogWarning($"Cart empty for user {userId} during checkout");
                    TempData["error"] = "Your cart is empty!";
                    return RedirectToAction("Index", "Cart");
                }

                // Validate payment method specific fields
                if (model.PaymentMethod == "Online Payment")
                {
                    _logger.LogInformation("Validating online payment fields");

                    if (string.IsNullOrEmpty(model.CardNumber) || 
                        string.IsNullOrEmpty(model.CardHolderName) ||
                        !model.ExpiryMonth.HasValue || 
                        !model.ExpiryYear.HasValue ||
                        string.IsNullOrEmpty(model.CVV))
                    {
                        ModelState.AddModelError("", "Please provide all card details for online payment.");
                        ViewBag.CartItems = items;
                        ViewBag.Total = items.Sum(i => i.TotalPrice);
                        _logger.LogWarning("Incomplete card details");
                        return View("Index", model);
                    }

                    // Simple card validation
                    if (model.CardNumber.Length < 13 || model.CardNumber.Length > 16)
                    {
                        ModelState.AddModelError("CardNumber", "Invalid card number. Must be 13-16 digits.");
                        ViewBag.CartItems = items;
                        ViewBag.Total = items.Sum(i => i.TotalPrice);
                        _logger.LogWarning($"Invalid card number length: {model.CardNumber.Length}");
                        return View("Index", model);
                    }

                    if (model.CVV.Length < 3 || model.CVV.Length > 4)
                    {
                        ModelState.AddModelError("CVV", "Invalid CVV. Must be 3-4 digits.");
                        ViewBag.CartItems = items;
                        ViewBag.Total = items.Sum(i => i.TotalPrice);
                        _logger.LogWarning($"Invalid CVV length: {model.CVV.Length}");
                        return View("Index", model);
                    }

                    // Validate expiry date
                    if (model.ExpiryYear < DateTime.Now.Year || 
                        (model.ExpiryYear == DateTime.Now.Year && model.ExpiryMonth < DateTime.Now.Month))
                    {
                        ModelState.AddModelError("ExpiryMonth", "Card has expired.");
                        ViewBag.CartItems = items;
                        ViewBag.Total = items.Sum(i => i.TotalPrice);
                        _logger.LogWarning("Expired card");
                        return View("Index", model);
                    }
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.CartItems = items;
                    ViewBag.Total = items.Sum(i => i.TotalPrice);
                    _logger.LogWarning("ModelState invalid: {0}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return View("Index", model);
                }

                // Validate stock availability
                foreach (var item in items)
                {
                    int availableStock = item.Product?.Stock ?? item.MobileAccessory?.Stock ?? 0;
                    if (availableStock < item.Quantity)
                    {
                        TempData["error"] = $"{item.ItemName} is out of stock. Available: {availableStock}";
                        ViewBag.CartItems = items;
                        ViewBag.Total = items.Sum(i => i.TotalPrice);
                        _logger.LogWarning($"Insufficient stock for {item.ItemName}");
                        return View("Index", model);
                    }
                }

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    Status = "Processing",
                    TotalAmount = items.Sum(i => i.TotalPrice),
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = model.PaymentMethod == "Cash on Delivery" ? "Pending" : "Paid",
                    TransactionId = model.PaymentMethod == "Online Payment" 
                        ? "TXN" + DateTime.Now.Ticks.ToString() 
                        : null,
                    CustomerName = model.CustomerName,
                    CustomerPhone = model.CustomerPhone,
                    ShippingAddress = model.ShippingAddress,
                    Items = new List<OrderItem>()
                };

                // Add order items
                foreach (var it in items)
                {
                    if (it.ProductId.HasValue)
                    {
                        order.Items!.Add(new OrderItem 
                        { 
                            ProductId = it.ProductId.Value, 
                            Quantity = it.Quantity, 
                            UnitPrice = it.ItemPrice
                        });

                        // Reduce product stock
                        var product = await _db.Products.FindAsync(it.ProductId.Value);
                        if (product != null)
                        {
                            product.Stock -= it.Quantity;
                            _db.Products.Update(product);
                        }
                    }
                    // Note: Accessories are not included in orders yet
                }

                _db.Orders.Add(order);
                _db.CartItems.RemoveRange(items);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Order {order.Id} placed successfully for user {userId}");

                TempData["success"] = model.PaymentMethod == "Online Payment" 
                    ? "Payment successful! Order placed." 
                    : "Order placed successfully! Pay on delivery.";
                
                return RedirectToAction("Success", new { id = order.Id });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error during checkout");
                TempData["error"] = "Database error. Please try again.";
                
                var items = await _db.CartItems
                    .Where(c => c.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
                    .Include(c => c.Product)
                    .Include(c => c.MobileAccessory)
                    .ToListAsync();
                
                ViewBag.CartItems = items;
                ViewBag.Total = items.Sum(i => i.TotalPrice);
                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order");
                TempData["error"] = $"Error placing order: {ex.Message}";
                
                var items = await _db.CartItems
                    .Where(c => c.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier))
                    .Include(c => c.Product)
                    .Include(c => c.MobileAccessory)
                    .ToListAsync();
                
                ViewBag.CartItems = items;
                ViewBag.Total = items.Sum(i => i.TotalPrice);
                return View("Index", model);
            }
        }

        public async Task<IActionResult> Success(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var order = await _db.Orders
                    .Include(o => o.Items!)
                    .ThenInclude(i => i.Product)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
                
                if (order == null)
                {
                    _logger.LogWarning($"Order {id} not found for user {userId}");
                    TempData["error"] = "Order not found.";
                    return RedirectToAction("Orders");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading success page");
                TempData["error"] = "Error loading order details.";
                return RedirectToAction("Orders");
            }
        }

        public async Task<IActionResult> Orders()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var orders = await _db.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.Items!)
                    .ThenInclude(i => i.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
                
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["error"] = "Error loading orders.";
                return View(new List<Order>());
            }
        }
    }
}
