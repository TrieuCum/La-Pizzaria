using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
using LaPizzaria.Services;
using LaPizzaria.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public sealed class OrderManagementController : Controller
    {
        private const int DefaultPageSize = 10;
        private static readonly string[] StatusKeys = { "Pending", "Confirmed", "Preparing", "Ready", "Delivering", "Completed", "Cancelled" };

        private readonly ApplicationDbContext _db;
        private readonly ShipperDispatchService _dispatch;
        private readonly IHubContext<OrderingHub> _hub;

        public OrderManagementController(ApplicationDbContext db, ShipperDispatchService dispatch, IHubContext<OrderingHub> hub)
        {
            _db = db;
            _dispatch = dispatch;
            _hub = hub;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index(string? search, string? statusFilter, int page = 1)
        {
            if (page < 1) page = 1;

            var query = _db.Orders.Include(o => o.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(o =>
                    (o.User != null && (
                        (o.User.FirstName != null && o.User.FirstName.ToLower().Contains(term)) ||
                        (o.User.LastName != null && o.User.LastName.ToLower().Contains(term)) ||
                        (o.User.UserName != null && o.User.UserName.ToLower().Contains(term)) ||
                        (o.User.Email != null && o.User.Email.ToLower().Contains(term)) ||
                        (o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(term))
                    )) ||
                    (o.DeliveryAddress != null && o.DeliveryAddress.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.OrderStatus == statusFilter);
            }

            var baseForCount = _db.Orders.Include(o => o.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                baseForCount = baseForCount.Where(o =>
                    (o.User != null && (
                        (o.User.FirstName != null && o.User.FirstName.ToLower().Contains(term)) ||
                        (o.User.LastName != null && o.User.LastName.ToLower().Contains(term)) ||
                        (o.User.UserName != null && o.User.UserName.ToLower().Contains(term)) ||
                        (o.User.Email != null && o.User.Email.ToLower().Contains(term)) ||
                        (o.User.PhoneNumber != null && o.User.PhoneNumber.Contains(term))
                    )) ||
                    (o.DeliveryAddress != null && o.DeliveryAddress.ToLower().Contains(term)));
            }
            var statusCounts = await baseForCount
                .GroupBy(o => o.OrderStatus)
                .Select(g => new { Status = g.Key ?? "", Count = g.Count() })
                .ToListAsync();
            var statusCountDict = StatusKeys.ToDictionary(k => k, _ => 0);
            foreach (var sc in statusCounts)
                if (!string.IsNullOrEmpty(sc.Status) && statusCountDict.ContainsKey(sc.Status))
                    statusCountDict[sc.Status] = sc.Count;

            var totalCount = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync();

            var model = new OrderManagementIndexViewModel
            {
                Orders = orders,
                Search = search,
                StatusFilter = statusFilter,
                Page = page,
                PageSize = DefaultPageSize,
                TotalCount = totalCount,
                StatusCounts = statusCountDict
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.OrderStatus != "Pending")
            {
                TempData["error"] = "Chỉ xác nhận đơn đang chờ xử lý.";
                return RedirectToAction(nameof(Index));
            }
            order.OrderStatus = "Confirmed";
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã xác nhận đơn hàng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? rejectReason)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.OrderStatus != "Pending")
            {
                TempData["error"] = "Chỉ từ chối đơn đang chờ xử lý.";
                return RedirectToAction(nameof(Index));
            }
            order.OrderStatus = "Cancelled";
            var reason = string.IsNullOrWhiteSpace(rejectReason) ? "Không nêu lý do" : rejectReason.Trim();
            order.Notes = string.IsNullOrWhiteSpace(order.Notes) ? $"[Từ chối: {reason}]" : order.Notes + " [Từ chối: " + reason + "]";
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã từ chối đơn hàng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (!StatusKeys.Contains(status))
            {
                TempData["error"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }
            order.OrderStatus = status;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // Auto-assign shipper only for delivery orders (must have DeliveryAddress)
            if (status == "Ready" && string.IsNullOrWhiteSpace(order.ShipperId)
                && !string.IsNullOrWhiteSpace(order.DeliveryAddress))
            {
                var assignedShipperId = await _dispatch.AutoAssignAsync(order.Id);
                if (assignedShipperId != null)
                {
                    // Fetch shipper display name for notification
                    var shipper = await _db.Users.FindAsync(assignedShipperId);
                    var shipperName = $"{shipper?.FirstName} {shipper?.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(shipperName)) shipperName = shipper?.UserName ?? assignedShipperId;

                    // Re-fetch order to get latest data for SignalR payload
                    var freshOrder = await _db.Orders.FindAsync(order.Id);
                    await _hub.Clients.Group($"shipper_{assignedShipperId}").SendAsync("newOrderAssigned", new
                    {
                        orderId = order.Id,
                        totalPrice = freshOrder?.TotalPrice ?? order.TotalPrice,
                        deliveryAddress = order.DeliveryAddress,
                        orderDate = order.OrderDate.ToString("HH:mm dd/MM/yyyy")
                    });
                    TempData["success"] = $"✅ Đơn #{order.Id} đã gán cho shipper {shipperName} (FIFO tự động).";
                }
                else
                {
                    TempData["success"] = "Đã cập nhật trạng thái. Không có shipper rảnh — shipper sẽ tự nhận đơn.";
                }
            }
            else
            {
                TempData["success"] = "Đã cập nhật trạng thái đơn hàng.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
