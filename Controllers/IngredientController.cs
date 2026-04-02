using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> Index(int? categoryId, string? q)
        {
            var query = _db.Ingredients
                .Include(i => i.Category)!.ThenInclude(c => c!.Parent)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                if (categoryId.Value == 0)
                    query = query.Where(i => i.CategoryId == null);
                else
                    query = query.Where(i => i.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(i => i.Name.Contains(term));
            }

            var list = await query.OrderBy(i => i.Name).ToListAsync();
            ViewBag.Categories = await _db.IngredientCategories
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
            ViewBag.CategoryId = categoryId;
            ViewBag.Query = q ?? string.Empty;
            return View(list);
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
            var c = await _db.IngredientCategories
                .Include(x => x.Children)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            foreach (var ch in c.Children.ToList())
            {
                ch.ParentId = null;
            }

            var ings = await _db.Ingredients.Where(i => i.CategoryId == id).ToListAsync();
            foreach (var i in ings)
                i.CategoryId = null;

            _db.IngredientCategories.Remove(c);
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã xóa nhóm (nguyên liệu chuyển sang \"Khác\").";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            ViewBag.Categories = await _db.IngredientCategories
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

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
                ViewBag.Categories = await _db.IngredientCategories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
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
