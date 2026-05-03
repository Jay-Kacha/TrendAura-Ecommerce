using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;

namespace trendaura.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _db.Products.Include(p => p.Category).ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name");
                    return View(product);
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    // Ensure the images directory exists
                    var uploadsDir = Path.Combine(_env.WebRootPath, "images");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    // Validate file extension
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("", "Only image files (jpg, jpeg, png, gif, webp) are allowed.");
                        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name");
                        return View(product);
                    }

                    // Validate file size (max 5MB)
                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "File size must be less than 5MB.");
                        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name");
                        return View(product);
                    }

                    var fileName = Guid.NewGuid().ToString() + fileExtension;
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fs);
                    }

                    product.ImageUrl = "/images/" + fileName;
                }

                _db.Products.Add(product);
                await _db.SaveChangesAsync();
                TempData["success"] = "Product created successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["error"] = $"Error creating product: {ex.Message}";
                ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name");
                return View(product);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
                    return View(product);
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    // Ensure the images directory exists
                    var uploadsDir = Path.Combine(_env.WebRootPath, "images");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    // Validate file extension
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("", "Only image files (jpg, jpeg, png, gif, webp) are allowed.");
                        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
                        return View(product);
                    }

                    // Validate file size (max 5MB)
                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "File size must be less than 5MB.");
                        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
                        return View(product);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                            catch { /* Ignore delete errors */ }
                        }
                    }

                    var fileName = Guid.NewGuid().ToString() + fileExtension;
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fs);
                    }

                    product.ImageUrl = "/images/" + fileName;
                }

                _db.Products.Update(product);
                await _db.SaveChangesAsync();
                TempData["success"] = "Product updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["error"] = $"Error updating product: {ex.Message}";
                ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var p = await _db.Products.FindAsync(id);
                if (p != null)
                {
                    // Delete all cart items referencing this product
                    var cartItems = await _db.CartItems
                        .Where(ci => ci.ProductId == id)
                        .ToListAsync();
                    _db.CartItems.RemoveRange(cartItems);

                    // Delete all wishlist items referencing this product
                    var wishlistItems = await _db.Wishlists
                        .Where(w => w.ProductId == id)
                        .ToListAsync();
                    _db.Wishlists.RemoveRange(wishlistItems);

                 
                    var reviews = await _db.AccessoryReviews
                        .Where(r => r.AccessoryId == id)
                        .ToListAsync();
                    _db.AccessoryReviews.RemoveRange(reviews);

                    // Delete all order items referencing this product
                    var orderItems = await _db.OrderItems
                        .Where(oi => oi.ProductId == id)
                        .ToListAsync();
                    _db.OrderItems.RemoveRange(orderItems);

                    // Delete image file if exists
                    if (!string.IsNullOrEmpty(p.ImageUrl))
                    {
                        var imagePath = Path.Combine(_env.WebRootPath, p.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            try
                            {
                                System.IO.File.Delete(imagePath);
                            }
                            catch { /* Ignore delete errors */ }
                        }
                    }

                    // Finally delete the product
                    _db.Products.Remove(p);
                    await _db.SaveChangesAsync();
                    TempData["success"] = "Product and related data deleted successfully!";
                }
                else
                {
                    TempData["error"] = "Product not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}