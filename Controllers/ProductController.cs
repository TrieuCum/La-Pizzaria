using Microsoft.AspNetCore.Mvc;
using LaPizzaria.ViewModels;
using LaPizzaria.Models; // Assuming you might need direct access to models later
using LaPizzaria.Data;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .Include(p => p.ProductIngredients)
                .ToListAsync();

            var allProductIngredients = await _db.ProductIngredients.ToListAsync();
            var ingredientsById = await _db.Ingredients.ToDictionaryAsync(i => i.Id);

            var outOfStockIds = new List<int>();
            foreach (var p in products)
            {
                var mappings = allProductIngredients.Where(pi => pi.ProductId == p.Id).ToList();
                if (!mappings.Any())
                {
                    continue;
                }
                var insufficient = mappings.Any(pi =>
                    ingredientsById.TryGetValue(pi.IngredientId, out var ing)
                        ? ing.StockQuantity < pi.QuantityPerUnit
                        : true
                );
                if (insufficient)
                {
                    outOfStockIds.Add(p.Id);
                }
            }

            var combos = await _db.Combos.Include(c => c.Items).ThenInclude(i => i.Product).ToListAsync();

            var vm = new LaPizzaria.ViewModels.ProductIndexViewModel
            {
                Products = products,
                Combos = combos,
                OutOfStockIds = outOfStockIds
            };

            return View(vm);
        }

        // Lightweight API for QR page product list
        [HttpGet("/api/products")]
        public async Task<IActionResult> ApiList()
        {
            var list = await _db.Products.Where(p => p.IsActive)
                .Select(p => new { id = p.Id, name = p.Name, price = p.Price })
                .ToListAsync();
            return Ok(list);
        }

        // Simple combos management (create combo with selected products)
        public IActionResult CreateCombo()
        {
            ViewBag.Products = _db.Products.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCombo(string name, decimal discountAmount, List<int> productId, List<int> minQuantity)
        {
            var combo = new Combo { Name = name, DiscountAmount = discountAmount, IsActive = true };
            foreach (var idx in System.Linq.Enumerable.Range(0, productId.Count))
            {
                combo.Items.Add(new ComboItem { ProductId = productId[idx], MinQuantity = idx < minQuantity.Count ? minQuantity[idx] : 1 });
            }
            _db.Combos.Add(combo);
            _db.SaveChanges();
            TempData["success"] = "Tạo combo thành công";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            ProductViewModel productViewModel = new ProductViewModel();
            if (id == null || id == 0)
            {
                return View(productViewModel);
            }
            var p = await _db.Products.FindAsync(id.Value);
            if (p == null) return NotFound();
            productViewModel = new ProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                Category = p.Category,
                IsActive = p.IsActive,
                IsCustomizable = p.IsCustomizable
            };
            return View(productViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ProductViewModel productViewModel)
        {
            if (ModelState.IsValid)
            {
                if (productViewModel.Id == 0)
                {
                    var p = new Product
                    {
                        Name = productViewModel.Name,
                        Description = productViewModel.Description,
                        Price = productViewModel.Price,
                        ImageUrl = productViewModel.ImageUrl,
                        Category = productViewModel.Category,
                        IsActive = productViewModel.IsActive,
                        IsCustomizable = productViewModel.IsCustomizable
                    };
                    _db.Products.Add(p);
                }
                else
                {
                    var p = await _db.Products.FindAsync(productViewModel.Id);
                    if (p == null) return NotFound();
                    p.Name = productViewModel.Name;
                    p.Description = productViewModel.Description;
                    p.Price = productViewModel.Price;
                    p.ImageUrl = productViewModel.ImageUrl;
                    p.Category = productViewModel.Category;
                    p.IsActive = productViewModel.IsActive;
                    p.IsCustomizable = productViewModel.IsCustomizable;
                    _db.Products.Update(p);
                }
                await _db.SaveChangesAsync();
                TempData["success"] = productViewModel.Id == 0 ? "Tạo sản phẩm thành công" : "Cập nhật sản phẩm thành công";
                return RedirectToAction("Index");
            }
            return View(productViewModel);
        }

        // Manage ingredients mapping for a product
        public async Task<IActionResult> Ingredients(int id)
        {
            var product = await _db.Products.Include(p => p.ProductIngredients).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            ViewBag.Product = product;
            var allIngredients = await _db.Ingredients.ToListAsync();
            var mapping = await _db.ProductIngredients.Where(pi => pi.ProductId == id).ToListAsync();
            ViewBag.Mapping = mapping;
            return View(allIngredients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveIngredients(int productId, List<int> ingredientId, List<decimal> quantityPerUnit)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var toRemove = _db.ProductIngredients.Where(pi => pi.ProductId == productId);
            _db.ProductIngredients.RemoveRange(toRemove);

            for (int i = 0; i < ingredientId.Count; i++)
            {
                var qty = i < quantityPerUnit.Count ? quantityPerUnit[i] : 0m;
                if (qty > 0)
                {
                    _db.ProductIngredients.Add(new ProductIngredient
                    {
                        ProductId = productId,
                        IngredientId = ingredientId[i],
                        QuantityPerUnit = qty
                    });
                }
            }

            await _db.SaveChangesAsync();
            TempData["success"] = "Cập nhật nguyên liệu cho món thành công";
            return RedirectToAction(nameof(Index));
        }
    }
}
