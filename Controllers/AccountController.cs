using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using trendaura.Models;
using trendaura.ViewModels;
using trendaura.Data;

namespace trendaura.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Assign Customer role
                await _userManager.AddToRoleAsync(user, "Customer");

                _logger.LogInformation($"New user registered: {model.Email}");
                TempData["success"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            _logger.LogInformation($"=== CLIENT LOGIN ATTEMPT ===");
            _logger.LogInformation($"Email: {model.Email}");

            if (!ModelState.IsValid)
            {
                TempData["error"] = "Please provide email and password.";
                return View(model);
            }

            // Sign in using Identity's default scheme (ClientCookie)
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation($"? Client '{model.Email}' logged in successfully!");
                TempData["success"] = "Welcome back!";

                if (!string.IsNullOrEmpty(model.ReturnUrl) && model.ReturnUrl.Contains("/Cart/Add", StringComparison.OrdinalIgnoreCase))
                {
                    
                    return RedirectToAction("Index", "Home");
                }
                // ------------------------------------

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                TempData["error"] = "Your account is locked. Please try again later.";
            }
            else
            {
                TempData["error"] = "Invalid email or password.";
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Sign out from Identity's default scheme (ClientCookie)
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Client logged out.");
            TempData["success"] = "Logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}