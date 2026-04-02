using LaPizzaria.Data;
using LaPizzaria.Helpers;
using LaPizzaria.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers;

/// <summary>API theo dõi vị trí shipper (polling, không WebSocket).</summary>
[Authorize]
[ApiController]
[Route("api/orders")]
public class OrdersTrackingApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersTrackingApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private async Task<bool> CanAccessOrderAsync(Order order, string? userId)
    {
        if (order == null || string.IsNullOrEmpty(userId)) return false;
        if (order.UserId == userId) return true;
        if (User.IsInRole("Admin") || User.IsInRole("Staff")) return true;
        return false;
    }

    /// <summary>GET /api/orders/{id}/tracking — dữ liệu ban đầu cho bản đồ.</summary>
    [HttpGet("{id:int}/tracking")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetTracking(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound();

        if (!await CanAccessOrderAsync(order, userId))
            return Forbid();

        var canShowMap = OrderTrackingHelper.CanShowShipperMap(order);

        return Ok(new
        {
            orderId = order.Id,
            canShowMap,
            deliveryStatus = order.DeliveryStatus,
            orderStatus = order.OrderStatus,
            shipperLat = order.ShipperLatitude,
            shipperLng = order.ShipperLongitude,
            customerLat = order.Latitude,
            customerLng = order.Longitude,
            deliveryAddress = order.DeliveryAddress,
            shipperUpdatedAtUtc = order.ShipperLocationUpdatedAt
        });
    }

    /// <summary>GET /api/orders/{id}/shipper-location — polling nhẹ (5–10s gọi một lần).</summary>
    [HttpGet("{id:int}/shipper-location")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetShipperLocation(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound();

        if (!await CanAccessOrderAsync(order, userId))
            return Forbid();

        if (!OrderTrackingHelper.CanShowShipperMap(order))
        {
            return Ok(new
            {
                active = false,
                lat = (double?)null,
                lng = (double?)null,
                updatedAt = (DateTime?)null
            });
        }

        return Ok(new
        {
            active = true,
            lat = order.ShipperLatitude,
            lng = order.ShipperLongitude,
            updatedAt = order.ShipperLocationUpdatedAt
        });
    }
}
