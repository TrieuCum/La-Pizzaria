using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Services;

/// <summary>
/// Logic điều phối shipper FIFO:
/// 1. Lọc shippers đang rảnh (không có đơn đang delivering).
/// 2. FIFO: shipper chờ lâu nhất (AssignedAt cũ nhất hoặc không có đơn nào gần nhất).
/// 3. Tie-break: số đơn hoàn thành hôm nay ít hơn → ưu tiên.
/// Trả về ShipperId được chọn, hoặc null nếu không có shipper rảnh.
/// </summary>
public class ShipperDispatchService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ShipperDispatchService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<string?> PickBestShipperAsync()
    {
        var shipperRole = "Shipper";
        var allShippers = await _userManager.GetUsersInRoleAsync(shipperRole);
        var activeShipperIds = allShippers.Where(s => s.IsActive).Select(s => s.Id).ToList();
        if (!activeShipperIds.Any()) return null;

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Shippers busy: currently assigned (accepted but not started) OR actively delivering
        var busyShipperIds = await _db.Orders
            .Where(o => o.ShipperId != null
                        && activeShipperIds.Contains(o.ShipperId)
                        && (o.DeliveryStatus == "delivering" || o.DeliveryStatus == "assigned"))
            .Select(o => o.ShipperId!)
            .Distinct()
            .ToListAsync();

        var availableIds = activeShipperIds.Except(busyShipperIds).ToList();
        if (!availableIds.Any()) return null;

        // Today's completed orders per shipper
        var todayCompleted = await _db.Orders
            .Where(o => o.ShipperId != null
                        && activeShipperIds.Contains(o.ShipperId)
                        && o.OrderStatus == "Completed"
                        && o.DeliveredAt >= today && o.DeliveredAt < tomorrow)
            .GroupBy(o => o.ShipperId!)
            .Select(g => new { ShipperId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShipperId, x => x.Count);

        // Last delivery time per available shipper (when they last became free)
        var lastDeliveryTime = await _db.Orders
            .Where(o => o.ShipperId != null
                        && availableIds.Contains(o.ShipperId)
                        && (o.DeliveryStatus == "delivered" || o.OrderStatus == "Completed"))
            .GroupBy(o => o.ShipperId!)
            .Select(g => new { ShipperId = g.Key, LastDeliveredAt = g.Max(o => o.DeliveredAt ?? o.UpdatedAt) })
            .ToDictionaryAsync(x => x.ShipperId, x => x.LastDeliveredAt);

        // Pick the best shipper: FIFO (idle longest) → fewer orders today as tiebreaker
        var epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var best = availableIds
            .OrderBy(id => lastDeliveryTime.TryGetValue(id, out var t) ? t : epoch) // idle longest first
            .ThenBy(id => todayCompleted.TryGetValue(id, out var c) ? c : 0)        // fewer orders today first
            .FirstOrDefault();

        return best;
    }

    /// <summary>
    /// Assign the best available shipper to the given order.
    /// Returns the assigned shipperId on success, or null if no shipper is available.
    /// </summary>
    public async Task<string?> AutoAssignAsync(int orderId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null || order.ShipperId != null) return null;

        var shipperId = await PickBestShipperAsync();
        if (shipperId == null) return null;

        order.ShipperId = shipperId;
        order.AssignedAt = DateTime.UtcNow;
        order.DeliveryStatus = "assigned";
        order.OrderStatus = "Delivering";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return shipperId;
    }
}
