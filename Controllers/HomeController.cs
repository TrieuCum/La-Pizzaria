using System.Diagnostics;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace LaPizzaria.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Shipper đã đăng nhập (cookie còn) vào Trang chủ → chuyển sang Dashboard Shipper
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && await _userManager.IsInRoleAsync(user, "Shipper"))
                    return RedirectToAction("Index", "Shipper");
            }

            // Top ordered products by total quantity, only active ones
            // Fetch all active products in the 'Pizza' category
            var topProducts = await _db.Products
                .Where(p => p.IsActive && (p.Category == "Pizza" || p.Category.Contains("Pizza")))
                .Select(p => new TopProductVm
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    TotalOrdered = p.OrderDetails.Sum(od => od.Quantity),
                    Price = p.Price
                })
                .OrderByDescending(x => x.TotalOrdered)
                .ToListAsync();

            var activeCombos = await _db.Combos
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .Where(c => c.IsActive)
                .ToListAsync();

            var activeVouchers = await _db.Vouchers
                .Where(v => v.IsActive && (v.ExpiresAtUtc == null || v.ExpiresAtUtc > DateTime.UtcNow))
                .OrderByDescending(v => v.DiscountPercent)
                .Take(3)
                .ToListAsync();

            var vm = new HomeIndexViewModel
            {
                TopProducts = topProducts,
                ActiveCombos = activeCombos,
                Vouchers = activeVouchers
            };
            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
