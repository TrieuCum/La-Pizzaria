using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LaPizzaria.Data;
using LaPizzaria.Models;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class IngredientController : Controller
    {
        private readonly ApplicationDbContext _db;
        public IngredientController(ApplicationDbContext db)
        {
            _db = db;
        }

        private static HashSet<int> SelfAndDescendantCategoryIds(int rootId, List<IngredientCategory> all)
        {
            var set = new HashSet<int> { rootId };
            bool added;
            do
            {
                added = false;
                foreach (var c in all)
                {
                    if (c.ParentId.HasValue && set.Contains(c.ParentId.Value) && set.Add(c.Id))
                        added = true;
                }
            } while (added);
            return set;
        }

        public async Task<IActionResult> Index(int? categoryId, string? q)
        {
            var allCategories = await _db.IngredientCategories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
            var roots = allCategories.Where(c => c.ParentId == null).ToList();

            var query = _db.Ingredients.AsNoTracking().Include(i => i.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(i => i.Name.Contains(term));
            }
            if (categoryId is > 0)
            {
                var allowed = SelfAndDescendantCategoryIds(categoryId.Value, allCategories);
                query = query.Where(i => i.CategoryId != null && allowed.Contains(i.CategoryId.Value));
            }

            var ingredients = await query.OrderBy(i => i.Name).ToListAsync();
            ViewBag.CategoryRoots = roots;
            ViewBag.AllCategories = allCategories;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.Q = q ?? "";
            return View(ingredients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name, int? parentId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["error"] = "Tên nhóm không được để trống.";
                return RedirectToAction(nameof(Index));
            }
            var maxSort = await _db.IngredientCategories.MaxAsync(c => (int?)c.SortOrder) ?? 0;
            _db.IngredientCategories.Add(new IngredientCategory
            {
                Name = name.Trim(),
                ParentId = parentId,
                SortOrder = maxSort + 1
            });
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã thêm nhóm nguyên liệu.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _db.IngredientCategories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id);
            if (cat == null) return NotFound();
            if (cat.Children.Any())
            {
                TempData["error"] = "Xóa các nhóm con trước.";
                return RedirectToAction(nameof(Index));
            }
            var used = await _db.Ingredients.AnyAsync(i => i.CategoryId == id);
            if (used)
            {
                TempData["error"] = "Còn nguyên liệu trong nhóm này — gỡ gán hoặc đổi nhóm trước.";
                return RedirectToAction(nameof(Index));
            }
            _db.IngredientCategories.Remove(cat);
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã xóa nhóm.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            var categories = await _db.IngredientCategories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
            ViewBag.Categories = categories;
            if (id == null) return View(new Ingredient());
            var ingredient = await _db.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();
            return View(ingredient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Ingredient model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _db.IngredientCategories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
                return View(model);
            }
            if (model.Id == 0)
            {
                _db.Ingredients.Add(model);
            }
            else
            {
                _db.Ingredients.Update(model);
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var ingredient = await _db.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();
            _db.Ingredients.Remove(ingredient);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Stock(string? q, string? status)
        {
            var query = _db.Ingredients.AsNoTracking().Include(i => i.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(i => i.Name.Contains(q.Trim()));

            var allIngredients = await query.OrderBy(i => i.Name).ToListAsync();

            if (status == "low")
                allIngredients = allIngredients.Where(i => i.StockQuantity > 0 && i.StockQuantity <= i.ReorderLevel).ToList();
            else if (status == "out")
                allIngredients = allIngredients.Where(i => i.StockQuantity <= 0).ToList();
            else if (status == "ok")
                allIngredients = allIngredients.Where(i => i.StockQuantity > i.ReorderLevel).ToList();

            var totalStock = allIngredients.Count;
            var outOfStock = allIngredients.Count(i => i.StockQuantity <= 0);
            var lowStock = allIngredients.Count(i => i.StockQuantity > 0 && i.StockQuantity <= i.ReorderLevel);
            var okStock = allIngredients.Count(i => i.StockQuantity > i.ReorderLevel);
            var totalValue = allIngredients.Sum(i => i.StockQuantity * i.UnitPrice);

            ViewBag.Q = q ?? "";
            ViewBag.Status = status ?? "";
            ViewBag.TotalStock = totalStock;
            ViewBag.OutOfStock = outOfStock;
            ViewBag.LowStock = lowStock;
            ViewBag.OkStock = okStock;
            ViewBag.TotalValue = totalValue;
            return View(allIngredients);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int id, decimal adjustment, string reason)
        {
            var ingredient = await _db.Ingredients.FindAsync(id);
            if (ingredient == null) return NotFound();
            ingredient.StockQuantity = Math.Max(0, ingredient.StockQuantity + adjustment);
            await _db.SaveChangesAsync();
            TempData["success"] = $"Đã {(adjustment >= 0 ? "nhập" : "xuất")} {Math.Abs(adjustment)} {ingredient.Unit} {ingredient.Name}. Lý do: {reason}";
            return RedirectToAction(nameof(Stock));
        }

        // ─── Nhập hàng ───────────────────────────────────────────────────────
        public async Task<IActionResult> Import(string? q, int page = 1)
        {
            const int pageSize = 20;
            var query = _db.IngredientImports
                .Include(i => i.Ingredient)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(i => i.Ingredient!.Name.Contains(q) || (i.Supplier != null && i.Supplier.Contains(q)));

            var total = await query.CountAsync();
            var records = await query.OrderByDescending(i => i.ImportDate).ThenByDescending(i => i.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var ingredients = await _db.Ingredients.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
            ViewBag.Q = q ?? "";
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalRecords = total;
            ViewBag.Ingredients = ingredients;
            return View(records);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(int ingredientId, decimal quantity, decimal unitPrice, DateTime importDate, string? supplier, string? note)
        {
            if (quantity <= 0) { TempData["error"] = "Số lượng phải > 0."; return RedirectToAction(nameof(Import)); }
            var ingredient = await _db.Ingredients.FindAsync(ingredientId);
            if (ingredient == null) { TempData["error"] = "Không tìm thấy nguyên liệu."; return RedirectToAction(nameof(Import)); }

            _db.IngredientImports.Add(new IngredientImport
            {
                IngredientId = ingredientId,
                ImportDate = importDate,
                Quantity = quantity,
                UnitPrice = unitPrice,
                Supplier = supplier,
                Note = note
            });

            // Update stock
            ingredient.StockQuantity += quantity;

            // Update current unit price and record price history if changed
            if (unitPrice > 0 && unitPrice != ingredient.UnitPrice)
            {
                ingredient.UnitPrice = unitPrice;
                _db.IngredientPriceHistories.Add(new IngredientPriceHistory
                {
                    IngredientId = ingredientId,
                    RecordedDate = importDate.Date,
                    UnitPrice = unitPrice,
                    Note = $"Cập nhật từ phiếu nhập — NCC: {supplier}"
                });
            }

            await _db.SaveChangesAsync();
            TempData["success"] = $"Đã nhập {quantity} {ingredient.Unit} {ingredient.Name} từ {supplier ?? "N/A"}.";
            return RedirectToAction(nameof(Import));
        }

        // ─── Xuất hàng ───────────────────────────────────────────────────────
        public async Task<IActionResult> Export(string? q, int page = 1)
        {
            const int pageSize = 20;
            var query = _db.IngredientExports
                .Include(i => i.Ingredient)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(i => i.Ingredient!.Name.Contains(q));

            var total = await query.CountAsync();
            var records = await query.OrderByDescending(i => i.ExportDate).ThenByDescending(i => i.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var ingredients = await _db.Ingredients.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
            ViewBag.Q = q ?? "";
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalRecords = total;
            ViewBag.Ingredients = ingredients;
            return View(records);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(int ingredientId, decimal quantity, DateTime exportDate, string reason, string? note)
        {
            if (quantity <= 0) { TempData["error"] = "Số lượng phải > 0."; return RedirectToAction(nameof(Export)); }
            var ingredient = await _db.Ingredients.FindAsync(ingredientId);
            if (ingredient == null) { TempData["error"] = "Không tìm thấy nguyên liệu."; return RedirectToAction(nameof(Export)); }
            if (ingredient.StockQuantity < quantity) { TempData["error"] = $"Tồn kho không đủ (Còn: {ingredient.StockQuantity} {ingredient.Unit})."; return RedirectToAction(nameof(Export)); }

            _db.IngredientExports.Add(new IngredientExport
            {
                IngredientId = ingredientId,
                ExportDate = exportDate,
                Quantity = quantity,
                Reason = reason,
                Note = note
            });

            ingredient.StockQuantity -= quantity;
            await _db.SaveChangesAsync();
            TempData["success"] = $"Đã xuất {quantity} {ingredient.Unit} {ingredient.Name}. Lý do: {reason}.";
            return RedirectToAction(nameof(Export));
        }

        // ─── Lịch sử giá ─────────────────────────────────────────────────────
        public async Task<IActionResult> PriceHistory(int? ingredientId, int months = 3)
        {
            var ingredients = await _db.Ingredients.Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync();
            ViewBag.Ingredients = ingredients;
            ViewBag.SelectedIngredientId = ingredientId;
            ViewBag.Months = months;

            if (ingredientId == null)
                return View(new List<IngredientPriceHistory>());

            var from = DateTime.UtcNow.Date.AddMonths(-months);
            var history = await _db.IngredientPriceHistories
                .Where(h => h.IngredientId == ingredientId && h.RecordedDate >= from)
                .OrderBy(h => h.RecordedDate)
                .ToListAsync();

            // Monthly average
            var monthlyAvg = history
                .GroupBy(h => new { h.RecordedDate.Year, h.RecordedDate.Month })
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, AvgPrice = g.Average(x => (double)x.UnitPrice) })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToList();
            ViewBag.MonthlyAvg = monthlyAvg;

            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPrice(int ingredientId, decimal unitPrice, DateTime recordedDate, string? note)
        {
            if (unitPrice <= 0) { TempData["error"] = "Giá phải > 0."; return RedirectToAction(nameof(PriceHistory), new { ingredientId }); }
            _db.IngredientPriceHistories.Add(new IngredientPriceHistory
            {
                IngredientId = ingredientId,
                RecordedDate = recordedDate.Date,
                UnitPrice = unitPrice,
                Note = note
            });
            // Update ingredient unit price
            var ing = await _db.Ingredients.FindAsync(ingredientId);
            if (ing != null) ing.UnitPrice = unitPrice;
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã ghi nhận giá mới.";
            return RedirectToAction(nameof(PriceHistory), new { ingredientId });
        }
    }
}
