using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;
using trendaura.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace trendaura.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
    public class MobileAccessoriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<MobileAccessoriesController> _logger;

        public MobileAccessoriesController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment, ILogger<MobileAccessoriesController> logger)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // GET: Admin/MobileAccessories
        public async Task<IActionResult> Index()
        {
            try
            {
                var accessories = await _db.MobileAccessories
                    .Include(m => m.Category)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                return View(accessories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading mobile accessories");
                TempData["error"] = "Error loading mobile accessories. Please try again.";
                return View(new List<MobileAccessory>());
            }
        }

        // GET: Admin/MobileAccessories/Details/5
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

        // GET: Admin/MobileAccessories/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create form");
                TempData["error"] = "Error loading form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/MobileAccessories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MobileAccessory accessory, IFormFile? imageFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", accessory.CategoryId);
                    return View(accessory);
                }

                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Validate file extension
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("", "Only image files (jpg, jpeg, png, gif, webp) are allowed.");
                        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", accessory.CategoryId);
                        return View(accessory);
                    }

                    // Validate file size (max 5MB)
                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "File size must be less than 5MB.");
                        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", accessory.CategoryId);
                        return View(accessory);
                    }

                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    accessory.ImageUrl = "/images/" + uniqueFileName;
                }

                accessory.CreatedAt = DateTime.UtcNow;
                _db.MobileAccessories.Add(accessory);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Mobile accessory created: {accessory.Name}");
                TempData["success"] = "Mobile Accessory created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating mobile accessory");
                TempData["error"] = "Database error: Unable to save the accessory. Please check your data and try again.";
                ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", accessory.CategoryId);
                return View(accessory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mobile accessory");
                TempData["error"] = $"Error creating mobile accessory: {ex.Message}";
                ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", accessory.CategoryId);
                return View(accessory);
            }
        }

        // GET: Admin/MobileAccessories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var accessory = await _db.MobileAccessories.FindAsync(id);
                if (accessory == null)
                {
                    return NotFound();
                }

                ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", accessory.CategoryId);
                return View(accessory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form");
                TempData["error"] = "Error loading edit form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/MobileAccessories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MobileAccessory accessory, IFormFile? imageFile)
        {
            if (id != accessory.Id)
            {
                return NotFound();
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", accessory.CategoryId);
                    return View(accessory);
                }

                var existingAccessory = await _db.MobileAccessories.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
                
                if (existingAccessory == null)
                {
                    return NotFound();
                }

                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Validate file extension
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("", "Only image files (jpg, jpeg, png, gif, webp) are allowed.");
                        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", accessory.CategoryId);
                        return View(accessory);
                    }

                    // Validate file size (max 5MB)
                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "File size must be less than 5MB.");
                        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", accessory.CategoryId);
                        return View(accessory);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingAccessory.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingAccessory.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                            catch (Exception deleteEx)
                            {
                                _logger.LogWarning(deleteEx, "Could not delete old image");
                            }
                        }
                    }

                    // Upload new image
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    accessory.ImageUrl = "/images/" + uniqueFileName;
                }
                else
                {
                    accessory.ImageUrl = existingAccessory.ImageUrl;
                }

                accessory.CreatedAt = existingAccessory.CreatedAt;
                accessory.UpdatedAt = DateTime.UtcNow;

                _db.MobileAccessories.Update(accessory);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Mobile accessory updated: {accessory.Name}");
                TempData["success"] = "Mobile Accessory updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!MobileAccessoryExists(accessory.Id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating mobile accessory");
                    TempData["error"] = "Concurrency error: The accessory was modified by another user. Please try again.";
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating mobile accessory");
                TempData["error"] = "Database error: Unable to update the accessory. Please check your data and try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mobile accessory");
                TempData["error"] = $"Error updating mobile accessory: {ex.Message}";
            }

            ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "Id", "Name", accessory.CategoryId);
            return View(accessory);
        }

        // GET: Admin/MobileAccessories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var accessory = await _db.MobileAccessories
                    .Include(m => m.Category)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (accessory == null)
                {
                    return NotFound();
                }

                return View(accessory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete confirmation");
                TempData["error"] = "Error loading delete confirmation.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/MobileAccessories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var accessory = await _db.MobileAccessories.FindAsync(id);
                
                if (accessory != null)
                {
                    // Delete image if exists
                    if (!string.IsNullOrEmpty(accessory.ImageUrl))
                    {
                        string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, accessory.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            try
                            {
                                System.IO.File.Delete(imagePath);
                            }
                            catch (Exception deleteEx)
                            {
                                _logger.LogWarning(deleteEx, "Could not delete image file");
                            }
                        }
                    }

                    _db.MobileAccessories.Remove(accessory);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation($"Mobile accessory deleted: {accessory.Name}");
                    TempData["success"] = "Mobile Accessory deleted successfully!";
                }
                else
                {
                    TempData["error"] = "Mobile Accessory not found.";
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting mobile accessory");
                TempData["error"] = "Database error: Unable to delete the accessory. It may be referenced by other records.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting mobile accessory");
                TempData["error"] = $"Error deleting mobile accessory: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MobileAccessoryExists(int id)
        {
            return _db.MobileAccessories.Any(e => e.Id == id);
        }
    }
}
