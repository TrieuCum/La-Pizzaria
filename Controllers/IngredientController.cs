using System.Linq;
using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using LaPizzaria.Models;

namespace LaPizzaria.Controllers
{
    public class IngredientController : Controller
    {
        private readonly ApplicationDbContext _db;
        public IngredientController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var ingredients = _db.Ingredients.ToList();
            return View(ingredients);
        }

        public IActionResult Upsert(int? id)
        {
            if (id == null) return View(new Ingredient());
            var ingredient = _db.Ingredients.Find(id);
            if (ingredient == null) return NotFound();
            return View(ingredient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Ingredient model)
        {
            if (!ModelState.IsValid) return View(model);
            if (model.Id == 0)
            {
                _db.Ingredients.Add(model);
            }
            else
            {
                _db.Ingredients.Update(model);
            }
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var ingredient = _db.Ingredients.Find(id);
            if (ingredient == null) return NotFound();
            _db.Ingredients.Remove(ingredient);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}


