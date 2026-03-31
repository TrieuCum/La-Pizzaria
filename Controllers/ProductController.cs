using Microsoft.AspNetCore.Mvc;
using LaPizzaria.ViewModels;
using LaPizzaria.Models; // Assuming you might need direct access to models later
using LaPizzaria.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Sanitizes image URLs: converts file:// URLs to /images/ paths
        /// </summary>
        private string? SanitizeImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return imageUrl?.Trim();

            var trimmed = imageUrl.Trim();
            // If it's a file:// URL, extract the filename and convert to web path
            if (trimmed.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
            {
                var filename = System.IO.Path.GetFileName(trimmed);
                return $"/images/{filename}";
            }

            return trimmed;
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
            var productStockStatus = new Dictionary<int, string>(); // safe / warning / danger

            foreach (var p in products)
            {
                var mappings = allProductIngredients.Where(pi => pi.ProductId == p.Id).ToList();
                if (!mappings.Any())
                {
                    // Không cấu hình nguyên liệu → coi như an toàn
                    productStockStatus[p.Id] = "safe";
                    continue;
                }

                // Tính trạng thái dựa theo từng nguyên liệu liên quan
                // > 50  : safe (An toàn)
                // > 20  : warning (Trung bình)
                // <= 20 : danger (Sắp hết)
                var aggregateStatus = "safe";

                foreach (var pi in mappings)
                {
                    if (!ingredientsById.TryGetValue(pi.IngredientId, out var ing))
                    {
                        aggregateStatus = "out";
                        break;
                    }

                    var qty = ing.StockQuantity;
                    var ingStatus = qty <= 0 ? "out" :
                                    qty <= 20 ? "danger" :
                                    qty <= 50 ? "warning" : "safe";

                    if (ingStatus == "out")
                    {
                        aggregateStatus = "out";
                        break;
                    }
                    if (ingStatus == "danger" && aggregateStatus != "out")
                    {
                        aggregateStatus = "danger";
                    }
                    if (ingStatus == "warning" && aggregateStatus == "safe")
                    {
                        aggregateStatus = "warning";
                    }
                }

                productStockStatus[p.Id] = aggregateStatus;

                // Giữ lại danh sách hết hàng (không đủ nguyên liệu để làm 1 phần)
                var insufficient = mappings.Any(pi =>
                    ingredientsById.TryGetValue(pi.IngredientId, out var ing2)
                        ? ing2.StockQuantity < pi.QuantityPerUnit
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
                OutOfStockIds = outOfStockIds,
                ProductStockStatus = productStockStatus
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
                .Select(p => new { id = p.Id, name = p.Name, price = p.Price, category = p.Category, imageUrl = p.ImageUrl })
                .ToListAsync();
            
            // Sanitize image URLs to convert file:// to web paths
            var sanitized = list.Select(p => new 
            { 
                p.id, 
                p.name, 
                p.price, 
                p.category, 
                imageUrl = SanitizeImageUrl(p.imageUrl) 
            }).ToList();
            
            return Ok(sanitized);
        }

        // Grouped menu for QR: products by category + combos as separate category
        [HttpGet("/api/menu")]
        [AllowAnonymous]
        public async Task<IActionResult> ApiMenu()
        {
            var productsRaw = await _db.Products.Where(p => p.IsActive)
                .Select(p => new { id = p.Id, name = p.Name, price = p.Price, category = p.Category, imageUrl = p.ImageUrl })
                .ToListAsync();

            var products = productsRaw.Select(p => new {
                p.id,
                p.name,
                p.price,
                p.category,
                imageUrl = SanitizeImageUrl(p.imageUrl)
            }).ToList();

            // Materialize combos and compute price on client to avoid EF translation issues
            var combosRaw = await _db.Combos.Where(c => c.IsActive)
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .ToListAsync();

            var combos = combosRaw.Select(c => new {
                id = c.Id,
                name = c.Name,
                imageUrl = SanitizeImageUrl(c.ImageUrl),
                price = (c.Items.Select(i => ((i.Product?.Price) ?? 0m) * Math.Max(1, i.MinQuantity)).Sum() - (c.DiscountAmount > 0 ? c.DiscountAmount : 0m)) * (1 - (c.DiscountPercent ?? 0m) / 100m),
                items = c.Items.Select(i => new { productId = i.ProductId, minQty = Math.Max(1, i.MinQuantity) })
            }).ToList();

            var result = new List<object>();
            var productGroups = products
                .GroupBy(p => p.category)
                .Select(g => new { key = g.Key, type = "product", items = g.Select(x => new { id = x.id, name = x.name, price = x.price, imageUrl = x.imageUrl }) });
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
            var productIdList = productId ?? new List<int>();
            var minQuantityList = minQuantity ?? new List<int>();
            var count = productIdList.Count;
            for (int i = 0; i < count; i++)
            {
                var pid = productIdList[i];
                var qty = i < minQuantityList.Count ? minQuantityList[i] : 1;
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
            ProductViewModel viewModel = new ProductViewModel();
            var allIngredients = await _db.Ingredients.OrderBy(i => i.Name).ToListAsync();
            var mappings = new List<ProductIngredient>();

            if (id != null && id != 0)
            {
                var p = await _db.Products.FindAsync(id.Value);
                if (p == null) return NotFound();
                
                viewModel.Id = p.Id;
                viewModel.Name = p.Name;
                viewModel.Description = p.Description;
                viewModel.Price = p.Price;
                viewModel.ImageUrl = p.ImageUrl;
                viewModel.Category = p.Category;
                viewModel.IsActive = p.IsActive;
                viewModel.IsCustomizable = p.IsCustomizable;

                mappings = await _db.ProductIngredients.Where(pi => pi.ProductId == id).ToListAsync();
            }

            viewModel.Ingredients = allIngredients.Select(i => new IngredientSelectionViewModel
            {
                IngredientId = i.Id,
                Name = i.Name,
                Unit = i.Unit,
                StockQuantity = i.StockQuantity,
                QuantityPerUnit = mappings.FirstOrDefault(m => m.IngredientId == i.Id)?.QuantityPerUnit ?? 0m
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ProductViewModel productViewModel)
        {
            if (ModelState.IsValid)
            {
                var p = productViewModel.Id == 0 ? new Product() : await _db.Products.FindAsync(productViewModel.Id);
                if (p == null) return NotFound();

                p.Name = productViewModel.Name;
                p.Description = productViewModel.Description;
                p.Price = productViewModel.Price;
                p.ImageUrl = productViewModel.ImageUrl;
                p.Category = productViewModel.Category;
                p.IsActive = productViewModel.IsActive;
                p.IsCustomizable = productViewModel.IsCustomizable;

                if (productViewModel.Id == 0)
                {
                    _db.Products.Add(p);
                }
                else
                {
                    _db.Products.Update(p);
                    // Clear old mappings
                    var oldMappings = _db.ProductIngredients.Where(pi => pi.ProductId == p.Id);
                    _db.ProductIngredients.RemoveRange(oldMappings);
                }

                await _db.SaveChangesAsync();

                // Save new mappings
                if (productViewModel.Ingredients != null)
                {
                    foreach (var item in productViewModel.Ingredients)
                    {
                        if (item.QuantityPerUnit > 0)
                        {
                            _db.ProductIngredients.Add(new ProductIngredient
                            {
                                ProductId = p.Id,
                                IngredientId = item.IngredientId,
                                QuantityPerUnit = item.QuantityPerUnit
                            });
                        }
                    }
                    await _db.SaveChangesAsync();
                }

                TempData["success"] = productViewModel.Id == 0 ? "Tạo sản phẩm thành công" : "Cập nhật sản phẩm thành công";
                return RedirectToAction("Index");
            }

            // Re-fetch ingredients if model state is invalid
            var ingredients = await _db.Ingredients.OrderBy(i => i.Name).ToListAsync();
            foreach (var item in productViewModel.Ingredients)
            {
                var ing = ingredients.FirstOrDefault(i => i.Id == item.IngredientId);
                if (ing != null)
                {
                    item.Name = ing.Name;
                    item.Unit = ing.Unit;
                    item.StockQuantity = ing.StockQuantity;
                }
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
