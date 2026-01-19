using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    public class MenuController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MenuController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            var query = _db.Products.Where(p => p.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(category) && category != "Tất cả")
            {
                query = query.Where(p => p.Category == category);
            }

            var products = await query.OrderBy(p => p.Category).ThenBy(p => p.Name).ToListAsync();
            var combos = await _db.Combos
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .Where(c => c.IsActive)
                .ToListAsync();

            var categories = await _db.Products
                .Where(p => p.IsActive)
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category ?? "Tất cả";
            ViewBag.Products = products;
            ViewBag.Combos = combos;

            return View();
        }
    }
}
