using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class TableController : Controller
    {
        private readonly ApplicationDbContext _db;
        public TableController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var tables = await _db.Tables.ToListAsync();
            var openOrders = await _db.Orders.Where(o => o.OrderStatus != "Completed").ToListAsync();
            var orderTables = await _db.OrderTables.ToListAsync();

            var tableById = tables.ToDictionary(t => t.Id);
            var orderToCodes = orderTables
                .GroupBy(ot => ot.OrderId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ot => tableById.ContainsKey(ot.TableId) ? tableById[ot.TableId].Code : $"T{ot.TableId}").ToList()
                );

            var tableAttachInfo = new Dictionary<int, string>();
            foreach (var t in tables)
            {
                var relatedOrders = orderTables.Where(ot => ot.TableId == t.Id).Select(ot => ot.OrderId).Distinct().ToList();
                var parts = new List<string>();
                foreach (var oid in relatedOrders)
                {
                    if (orderToCodes.TryGetValue(oid, out var codes))
                    {
                        parts.Add($"#${oid}: {string.Join(", ", codes)}");
                    }
                }
                tableAttachInfo[t.Id] = string.Join(" | ", parts);
            }

            var viewModel = new TableIndexViewModel
            {
                Tables = tables,
                OpenOrders = openOrders,
                TableAttachInfo = tableAttachInfo
            };

            return View(viewModel);
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
                TempData["success"] = $"Đã tạo bàn {model.Code}.";
            }
            else
            {
                _db.Tables.Update(model);
                TempData["success"] = $"Đã cập nhật bàn {model.Code}.";
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
            TempData["success"] = $"Đã xoá bàn {table.Code}.";
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
                var openOrderId = _db.OrderTables
                    .Include(ot => ot.Order)
                    .Where(ot => ot.TableId == id && ot.Order != null && ot.Order.OrderStatus != "Completed")
                    .Select(ot => ot.OrderId)
                    .FirstOrDefault();
                TempData["error"] = "Bàn chưa thanh toán. Vui lòng thanh toán trước khi dọn bàn.";
                if (openOrderId > 0)
                {
                    return RedirectToAction("Index", "Order", new { id = openOrderId });
                }
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


