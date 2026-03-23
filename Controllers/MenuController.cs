using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
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

        public async Task<IActionResult> Index(string? category, string? search)
        {
            var productsQuery = _db.Products.Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(category) && category != "All")
                productsQuery = productsQuery.Where(p => p.Category == category);

            var searchTrim = search?.Trim();
            if (!string.IsNullOrEmpty(searchTrim))
            {
                var term = searchTrim.ToLower();
                productsQuery = productsQuery.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            var products = await productsQuery.ToListAsync();

            var combosQuery = _db.Combos
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(searchTrim))
            {
                var term = searchTrim.ToLower();
                combosQuery = combosQuery.Where(c =>
                    c.Name.ToLower().Contains(term) ||
                    (c.Description != null && c.Description.ToLower().Contains(term)));
            }

            var combos = await combosQuery.ToListAsync();

            var viewModel = new MenuViewModel
            {
                Products = products,
                Combos = combos,
                Search = searchTrim
            };

            ViewBag.CurrentCategory = category ?? "All";
            ViewBag.Search = searchTrim;

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ProductDetails(int id)
        {
            var product = await _db.Products
                .Include(p => p.ProductToppings).ThenInclude(pt => pt.Topping)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            if (product == null)
                return NotFound();
            return View(product);
        }

        /// <summary>API gợi ý tìm kiếm: trả về danh sách product/combo khớp từ khóa (dùng cho dropdown thanh tìm kiếm).</summary>
        [HttpGet]
        public async Task<IActionResult> SearchSuggestions([FromQuery] string? q, [FromQuery] int limit = 6)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(Array.Empty<object>());

            var term = q.Trim().ToLower();
            var products = await _db.Products
                .Where(p => p.IsActive && (p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term))))
                .OrderBy(p => p.Name)
                .Take(limit)
                .Select(p => new { type = "product", id = p.Id, name = p.Name, price = p.Price, imageUrl = p.ImageUrl, category = p.Category })
                .ToListAsync();
            var combos = await _db.Combos
                .Where(c => c.IsActive && (c.Name.ToLower().Contains(term) || (c.Description != null && c.Description.ToLower().Contains(term))))
                .OrderBy(c => c.Name)
                .Take(limit)
                .Select(c => new { type = "combo", id = c.Id, name = c.Name, imageUrl = c.ImageUrl })
                .ToListAsync();

            var list = new List<object>();
            foreach (var p in products) list.Add(p);
            foreach (var c in combos) list.Add(c);
            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> ComboDetails(int id)
        {
            var combo = await _db.Combos
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            if (combo == null)
                return NotFound();
            return View(combo);
        }

        [HttpGet]
        public async Task<IActionResult> MixPizza(int? id1)
        {
            var pizzas = await _db.Products
                .Where(p => p.Category == "Pizza" && p.IsActive)
                .ToListAsync();
            ViewBag.SelectedId1 = id1;
            return View(pizzas);
        }
    }
}
