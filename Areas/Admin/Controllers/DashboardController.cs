using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trendaura.Data;

namespace trendaura.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var usersCount = await _db.Users.CountAsync();
            var productsCount = await _db.Products.CountAsync();
            var categoriesCount = await _db.Categories.CountAsync();
            var ordersCount = await _db.Orders.CountAsync();

            ViewBag.Users = usersCount;
            ViewBag.Products = productsCount;
            ViewBag.Categories = categoriesCount;
            ViewBag.Orders = ordersCount;

            var recentOrders = await _db.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();
            
            return View(recentOrders);
        }
    }
}
