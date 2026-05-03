using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using trendaura.Models;
using trendaura.ViewModels;
using System.Security.Claims;

namespace trendaura.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Check if already logged in as admin using AdminCookie
            if (HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var authType = HttpContext.User.Identity.AuthenticationType;
                if (authType == "AdminCookie" && User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new AdminLoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            _logger.LogInformation($"=== ADMIN LOGIN ATTEMPT ===");
            _logger.LogInformation($"Username entered: {model.Username}");

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please fill in all required fields.";
                return View(model);
            }

            try
            {
                // Find admin user by username
                var adminUser = await _userManager.FindByNameAsync(model.Username);
                
                if (adminUser == null)
                {
                    _logger.LogWarning($"? Admin user not found: {model.Username}");
                    TempData["error"] = "Invalid username or password.";
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View(model);
                }

                // Check if user has Admin role
                var isAdmin = await _userManager.IsInRoleAsync(adminUser, "Admin");
                if (!isAdmin)
                {
                    _logger.LogWarning($"? User '{model.Username}' is not an admin");
                    TempData["error"] = "Invalid username or password.";
                    return View(model);
                }

                // Verify password
                var result = await _signInManager.CheckPasswordSignInAsync(adminUser, model.Password, false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning($"? Invalid password for admin: {model.Username}");
                    TempData["error"] = "Invalid username or password.";
                    return View(model);
                }

                _logger.LogInformation($"? Authentication successful for admin: {model.Username}");

                // Create claims for admin
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, adminUser.UserName ?? model.Username),
                    new Claim(ClaimTypes.Email, adminUser.Email ?? "admin@trendaura.com"),
                    new Claim(ClaimTypes.NameIdentifier, adminUser.Id),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("FullName", adminUser.FullName ?? model.Username),
                    new Claim("UserType", "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, "AdminCookie");
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8),
                    AllowRefresh = true
                };

                // Sign in with ADMIN cookie scheme
                _logger.LogInformation("Signing in admin with AdminCookie scheme...");
                await HttpContext.SignInAsync(
                    "AdminCookie",
                    claimsPrincipal,
                    authProperties);

                _logger.LogInformation($"? Admin '{model.Username}' logged in successfully!");
                TempData["success"] = $"Welcome back, {adminUser.FullName ?? model.Username}!";
                
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"? Exception during admin login");
                TempData["error"] = "An error occurred during login. Please try again.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Sign out from ADMIN cookie only
            await HttpContext.SignOutAsync("AdminCookie");
            _logger.LogInformation("Admin logged out from AdminCookie.");
            TempData["success"] = "Logged out successfully.";
            return RedirectToAction("Login", "Auth", new { area = "Admin" });
        }
    }
}
