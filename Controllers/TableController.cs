using System.Linq;
using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.EntityFrameworkCore;

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
            ViewBag.OpenOrders = _db.Orders.Where(o => o.OrderStatus != "Completed").ToList();

            var orderTables = _db.OrderTables.ToList();
            var tableById = tables.ToDictionary(t => t.Id);
            var orderToCodes = orderTables
                .GroupBy(ot => ot.OrderId)
                .ToDictionary(g => g.Key, g => g.Select(ot => tableById.ContainsKey(ot.TableId) ? tableById[ot.TableId].Code : $"T{ot.TableId}").ToList());

            var tableAttachInfo = new System.Collections.Generic.Dictionary<int, string>();
            foreach (var t in tables)
            {
                var relatedOrders = orderTables.Where(ot => ot.TableId == t.Id).Select(ot => ot.OrderId).Distinct().ToList();
                var parts = new System.Collections.Generic.List<string>();
                foreach (var oid in relatedOrders)
                {
                    if (orderToCodes.TryGetValue(oid, out var codes))
                    {
                        parts.Add($"#${oid}: {string.Join(", ", codes)}");
                    }
                }
                tableAttachInfo[t.Id] = string.Join(" | ", parts);
            }
            ViewBag.TableAttachInfo = tableAttachInfo;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear(int id)
        {
            var table = _db.Tables.Find(id);
            if (table == null) return NotFound();

            // Không cho dọn nếu vẫn còn order chưa hoàn tất
            var hasOpenOrder = _db.OrderTables
                .Include(ot => ot.Order)
                .Any(ot => ot.TableId == id && ot.Order != null && ot.Order.OrderStatus != "Completed");
            if (hasOpenOrder)
            {
                TempData["error"] = "Bàn vẫn đang có order chưa thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            // Gỡ liên kết bàn khỏi các order đã xong và set trạng thái rảnh
            var attach = _db.OrderTables.Where(ot => ot.TableId == id).ToList();
            _db.OrderTables.RemoveRange(attach);
            table.IsOccupied = false;
            _db.SaveChanges();
            TempData["success"] = $"Đã dọn bàn {table.Code}.";
            return RedirectToAction(nameof(Index));
        }
    }
}


