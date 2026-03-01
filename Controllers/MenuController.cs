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

        public async Task<IActionResult> Index(string? category)
        {
            var productsQuery = _db.Products.Where(p => p.IsActive);
            
            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                productsQuery = productsQuery.Where(p => p.Category == category);
            }

            var products = await productsQuery.ToListAsync();
            var combos = await _db.Combos
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .Where(c => c.IsActive)
                .ToListAsync();
            var viewModel = new MenuViewModel
            {
                Products = products,
                Combos = combos
            };

            ViewBag.CurrentCategory = category ?? "All";

            return View(viewModel);
        }
    }
}
