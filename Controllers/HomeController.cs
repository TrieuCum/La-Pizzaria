using System.Diagnostics;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // Top ordered products by total quantity, only active ones
            var topProducts = await _db.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.Product != null && od.Product.IsActive)
                .GroupBy(od => od.Product!)
                .Select(g => new TopProductVm
                {
                    ProductId = g.Key.Id,
                    Name = g.Key.Name,
                    Description = g.Key.Description,
                    ImageUrl = g.Key.ImageUrl,
                    TotalOrdered = g.Sum(x => x.Quantity),
                    Price = g.Key.Price
                })
                .OrderByDescending(x => x.TotalOrdered)
                .Take(6)
                .ToListAsync();

            var activeCombos = await _db.Combos
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .Where(c => c.IsActive)
                .ToListAsync();

            var vm = new HomeIndexViewModel
            {
                TopProducts = topProducts,
                ActiveCombos = activeCombos
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
