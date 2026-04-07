using System.Security.Claims;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    [ApiController]
    [Route("api/shipper")]
    [Authorize]
    public sealed class ShipperLocationController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ShipperLocationController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("location")]
        public async Task<IActionResult> GetLocation([FromQuery] int orderId)
        {
            if (orderId <= 0)
            {
                return BadRequest(new { message = "orderId không hợp lệ." });
            }

            var order = await _db.Orders
                .AsNoTracking()
                .Select(o => new
                {
                    o.Id,
                    o.UserId,
                    o.ShipperLatitude,
                    o.ShipperLongitude,
                    o.DeliveryStatus,
                    o.OrderStatus
                })
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn hàng." });
            }

            if (!CanViewOrder(order.UserId))
            {
                return Forbid();
            }

            if (!order.ShipperLatitude.HasValue || !order.ShipperLongitude.HasValue)
            {
                return Ok((object?)null);
            }

            return Ok(new
            {
                latitude = order.ShipperLatitude.Value,
                longitude = order.ShipperLongitude.Value,
                deliveryStatus = order.DeliveryStatus,
                orderStatus = order.OrderStatus
            });
        }

        [HttpPost("update-location")]
        [Authorize(Roles = "Shipper")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
        {
            if (request == null || request.OrderId <= 0)
            {
                return BadRequest(new { message = "Thiếu dữ liệu cập nhật vị trí." });
            }

            if (request.Latitude < -90 || request.Latitude > 90 || request.Longitude < -180 || request.Longitude > 180)
            {
                return BadRequest(new { message = "Tọa độ không hợp lệ." });
            }

            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
            {
                return Unauthorized(new { message = "Phiên đăng nhập không hợp lệ." });
            }

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId);
            if (order == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn hàng." });
            }

            if (!string.Equals(order.ShipperId, shipperId, StringComparison.Ordinal))
            {
                return Forbid();
            }

            var status = order.DeliveryStatus ?? string.Empty;
            var canUpdate = string.Equals(status, "assigned", StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, "delivering", StringComparison.OrdinalIgnoreCase);
            if (!canUpdate)
            {
                return Conflict(new { message = "Shipper chỉ được cập nhật vị trí khi đơn đang giao." });
            }

            order.ShipperLatitude = request.Latitude;
            order.ShipperLongitude = request.Longitude;
            order.ShipperLocationUpdatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                latitude = order.ShipperLatitude,
                longitude = order.ShipperLongitude,
                updatedAt = order.ShipperLocationUpdatedAt
            });
        }

        private bool CanViewOrder(string? orderUserId)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Staff"))
            {
                return true;
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return false;
            }

            return string.Equals(orderUserId, currentUserId, StringComparison.Ordinal);
        }

        public sealed class UpdateLocationRequest
        {
            public int OrderId { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}
