using System.Linq;
using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using LaPizzaria.Models;

namespace LaPizzaria.Controllers
{
    public class TableController : Controller
    {
        private readonly ApplicationDbContext _db;
        public TableController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var tables = _db.Tables.ToList();
            return View(tables);
        }

        public IActionResult Upsert(int? id)
        {
            if (id == null) return View(new Table());
            var table = _db.Tables.Find(id);
            if (table == null) return NotFound();
            return View(table);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Table model)
        {
            if (!ModelState.IsValid) return View(model);
            if (model.Id == 0)
            {
                _db.Tables.Add(model);
            }
            else
            {
                _db.Tables.Update(model);
            }
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var table = _db.Tables.Find(id);
            if (table == null) return NotFound();
            _db.Tables.Remove(table);
            _db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
    }
}


