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
    }
}
