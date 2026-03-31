using LaPizzaria.Data;
using LaPizzaria.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Shipper")]
    public class ShipperController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ShipperController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["ShipperNav"] = "Index";

            var baseQuery = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => !string.IsNullOrWhiteSpace(o.DeliveryAddress));

            var activeOrders = await baseQuery
                .Where(o => o.OrderStatus == "Delivering")
                .OrderByDescending(o => o.OrderDate)
                .Take(1)
                .ToListAsync();

            var newOrders = await baseQuery
                .Where(o => o.OrderStatus == "Pending" || o.OrderStatus == "Confirmed" || o.OrderStatus == "Ready")
                .OrderBy(o => o.OrderDate)
                .Take(8)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var todaysDelivered = await _db.Orders
                .Where(o => o.OrderStatus == "Completed" && o.UpdatedAt.Date == today)
                .ToListAsync();

            var vm = new ShipperIndexViewModel
            {
                ActiveOrder = activeOrders.Select(MapToSummary).FirstOrDefault(),
                NewOrders = newOrders.Select(MapToSummary).ToList(),
                DeliveredTodayCount = todaysDelivered.Count,
                IncomeToday = todaysDelivered.Sum(o => o.TotalPrice)
            };

            return View(vm);
        }

        public async Task<IActionResult> History(string status = "All", DateTime? fromDate = null, DateTime? toDate = null, int page = 1)
        {
            ViewData["ShipperNav"] = "History";
            if (page < 1) page = 1;
            const int pageSize = 9;

            var query = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => !string.IsNullOrWhiteSpace(o.DeliveryAddress));

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(o => o.UpdatedAt >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(o => o.UpdatedAt < toExclusive);
            }

            var totalCount = await query.CountAsync();
            var list = await query
                .OrderByDescending(o => o.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var income = await query
                .Where(o => o.OrderStatus == "Completed")
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;

            var vm = new ShipperHistoryViewModel
            {
                Orders = list.Select(MapToSummary).ToList(),
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalIncome = income
            };

            return View(vm);
        }

        public IActionResult Income()
        {
            ViewData["ShipperNav"] = "Income";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || string.IsNullOrWhiteSpace(order.DeliveryAddress))
                return NotFound();

            var vm = new ShipperOrderDetailViewModel
            {
                OrderId = order.Id,
                OrderStatus = order.OrderStatus,
                CustomerName = GetCustomerName(order),
                CustomerPhone = order.User?.PhoneNumber,
                DeliveryAddress = order.DeliveryAddress ?? string.Empty,
                Note = order.Notes,
                PaymentMethod = order.PaymentMethod,
                OrderDate = order.OrderDate,
                TotalPrice = order.TotalPrice,
                Latitude = order.Latitude,
                Longitude = order.Longitude,
                Items = order.OrderDetails.Select(d => new ShipperOrderItemViewModel
                {
                    ProductName = d.Product?.Name ?? "Sản phẩm",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal,
                    Size = d.Size
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.OrderStatus is "Pending" or "Confirmed" or "Ready")
            {
                order.OrderStatus = "Delivering";
                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.OrderStatus == "Delivering")
            {
                order.OrderStatus = "Completed";
                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnOrder(int id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.OrderStatus != "Completed")
            {
                order.OrderStatus = "Cancelled";
                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private static ShipperOrderSummaryViewModel MapToSummary(Models.Order order)
        {
            var firstItem = order.OrderDetails.FirstOrDefault();
            var itemCount = order.OrderDetails.Sum(d => d.Quantity);
            return new ShipperOrderSummaryViewModel
            {
                OrderId = order.Id,
                OrderStatus = order.OrderStatus,
                CustomerName = GetCustomerName(order),
                CustomerPhone = order.User?.PhoneNumber,
                DeliveryAddress = order.DeliveryAddress ?? "Chưa có địa chỉ",
                ItemTitle = firstItem?.Product?.Name ?? "Đơn hàng",
                ItemCount = itemCount,
                OrderDate = order.OrderDate,
                TotalPrice = order.TotalPrice
            };
        }

        private static string GetCustomerName(Models.Order order)
        {
            var first = order.User?.FirstName?.Trim();
            var last = order.User?.LastName?.Trim();
            var full = $"{first} {last}".Trim();
            return string.IsNullOrWhiteSpace(full) ? (order.User?.UserName ?? "Khách hàng") : full;
        }
    }
}