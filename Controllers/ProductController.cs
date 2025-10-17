using Microsoft.AspNetCore.Mvc;
using LaPizzaria.ViewModels;
using LaPizzaria.Models; // Assuming you might need direct access to models later
using LaPizzaria.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LaPizzaria.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index(string? q, string? category, string? status)
        {
            var query = _db.Products.Include(p => p.ProductIngredients).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(p => p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả danh mục")
            {
                query = query.Where(p => p.Category == category);
            }
            var products = await query.ToListAsync();

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

            if (!string.IsNullOrWhiteSpace(status) && status != "Tất cả trạng thái")
            {
                if (status == "Đang bán")
                {
                    products = products.Where(p => p.IsActive && !outOfStockIds.Contains(p.Id)).ToList();
                }
                else if (status == "Hết hàng")
                {
                    products = products.Where(p => outOfStockIds.Contains(p.Id)).ToList();
                }
                else if (status == "Ngừng bán")
                {
                    products = products.Where(p => !p.IsActive).ToList();
                }
            }

            var combos = await _db.Combos.Include(c => c.Items).ThenInclude(i => i.Product).ToListAsync();

            var vm = new LaPizzaria.ViewModels.ProductIndexViewModel
            {
                Products = products,
                Combos = combos,
                OutOfStockIds = outOfStockIds
            };

            ViewBag.Query = q ?? string.Empty;
            ViewBag.Category = category ?? "Tất cả danh mục";
            ViewBag.Status = status ?? "Tất cả trạng thái";

            return View(vm);
        }

        // Lightweight API for QR page product list
        [HttpGet("/api/products")]
        [AllowAnonymous]
        public async Task<IActionResult> ApiList()
        {
            var list = await _db.Products.Where(p => p.IsActive)
                .Select(p => new { id = p.Id, name = p.Name, price = p.Price })
                .ToListAsync();
            return Ok(list);
        }

        // Grouped menu for QR: products by category + combos as separate category
        [HttpGet("/api/menu")]
        [AllowAnonymous]
        public async Task<IActionResult> ApiMenu()
        {
            var products = await _db.Products.Where(p => p.IsActive)
                .Select(p => new { id = p.Id, name = p.Name, price = p.Price, category = p.Category })
                .ToListAsync();

            var combos = await _db.Combos.Where(c => c.IsActive)
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .Select(c => new {
                    id = c.Id,
                    name = c.Name,
                    imageUrl = c.ImageUrl,
                    // compute price from items minus amount and percent
                    price = (c.Items.Select(i => (i.Product != null ? i.Product.Price : 0m) * Math.Max(1, i.MinQuantity)).Sum() - (c.DiscountAmount > 0 ? c.DiscountAmount : 0)) * (1 - (c.DiscountPercent ?? 0m)/100m),
                    items = c.Items.Select(i => new { productId = i.ProductId, minQty = Math.Max(1, i.MinQuantity) })
                })
                .ToListAsync();

            var result = new List<object>();
            var productGroups = products
                .GroupBy(p => p.category)
                .Select(g => new { key = g.Key, type = "product", items = g.Select(x => new { id = x.id, name = x.name, price = x.price }) });
            result.AddRange(productGroups);
            result.Add(new { key = "Combo", type = "combo", items = combos.Select(c => new { id = c.id, name = c.name, price = c.price, items = c.items }) });

            return Ok(result);
        }

        // Simple combos management (create combo with selected products)
        public IActionResult CreateCombo()
        {
            ViewBag.Products = _db.Products.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCombo(string name, decimal discountAmount, decimal? discountPercent, string? imageUrl, List<int> productId, List<int> minQuantity)
        {
            var combo = new Combo { Name = name, DiscountAmount = discountAmount, DiscountPercent = discountPercent, ImageUrl = imageUrl, IsActive = true };
            foreach (var idx in System.Linq.Enumerable.Range(0, productId.Count))
            {
                combo.Items.Add(new ComboItem { ProductId = productId[idx], MinQuantity = idx < minQuantity.Count ? minQuantity[idx] : 1 });
            }
            _db.Combos.Add(combo);
            _db.SaveChanges();
            TempData["success"] = "Tạo combo thành công";
            return RedirectToAction("Index");
        }

        // Edit combo
        public async Task<IActionResult> EditCombo(int id)
        {
            var combo = await _db.Combos.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id);
            if (combo == null) return NotFound();
            ViewBag.Products = await _db.Products.ToListAsync();
            return View(combo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCombo(int id, string name, decimal discountAmount, decimal? discountPercent, string? imageUrl, List<int> productId, List<int> minQuantity)
        {
            var combo = await _db.Combos.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id);
            if (combo == null) return NotFound();

            combo.Name = name;
            combo.DiscountAmount = discountAmount;
            combo.DiscountPercent = discountPercent;
            combo.ImageUrl = imageUrl;

            // replace items
            var existing = combo.Items.ToList();
            if (existing.Count > 0)
            {
                _db.RemoveRange(existing);
            }
            combo.Items.Clear();
            var count = productId?.Count ?? 0;
            for (int i = 0; i < count; i++)
            {
                var pid = productId[i];
                var qty = i < (minQuantity?.Count ?? 0) ? minQuantity[i] : 1;
                if (pid > 0 && qty > 0)
                {
                    combo.Items.Add(new ComboItem { ProductId = pid, MinQuantity = qty });
                }
            }
            await _db.SaveChangesAsync();
            TempData["success"] = "Cập nhật combo thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCombo(int id)
        {
            var combo = await _db.Combos.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id);
            if (combo == null) return NotFound();
            if (combo.Items.Any())
            {
                _db.RemoveRange(combo.Items);
            }
            _db.Combos.Remove(combo);
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã xoá combo";
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
