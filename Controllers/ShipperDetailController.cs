using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LaPizzaria.Models;
using LaPizzaria.Data;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ShipperDetailController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShipperDetailController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var shipperRole = "Shipper";
            var shippersInRole = await _userManager.GetUsersInRoleAsync(shipperRole);
            var shipperIds = shippersInRole.Select(s => s.Id).ToList();

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Load all shipper orders (completed + active)
            var allShipperOrders = await _db.Orders
                .Where(o => o.ShipperId != null && shipperIds.Contains(o.ShipperId))
                .ToListAsync();

            var shipperStats = shippersInRole.Select(s => new ShipperStatViewModel
            {
                ShipperId = s.Id,
                FullName = $"{s.FirstName} {s.LastName}".Trim().Length > 0
                    ? $"{s.FirstName} {s.LastName}".Trim()
                    : s.UserName ?? s.Email ?? "Shipper",
                Email = s.Email ?? "",
                Phone = s.PhoneNumber ?? "",
                AvatarUrl = s.AvatarUrl,
                IsActive = s.IsActive,
                TotalCompleted = allShipperOrders.Count(o => o.ShipperId == s.Id && o.OrderStatus == "Completed"),
                TodayCompleted = allShipperOrders.Count(o => o.ShipperId == s.Id && o.OrderStatus == "Completed" && o.DeliveredAt >= today && o.DeliveredAt < tomorrow),
                ActiveOrder = allShipperOrders.FirstOrDefault(o => o.ShipperId == s.Id && o.DeliveryStatus == "delivering"),
                CurrentLatitude = allShipperOrders.Where(o => o.ShipperId == s.Id && o.DeliveryStatus == "delivering").Select(o => o.ShipperLatitude).FirstOrDefault(),
                CurrentLongitude = allShipperOrders.Where(o => o.ShipperId == s.Id && o.DeliveryStatus == "delivering").Select(o => o.ShipperLongitude).FirstOrDefault()
            }).OrderByDescending(s => s.TodayCompleted).ToList();

            return View(shipperStats);
        }

        [HttpGet]
        public async Task<IActionResult> ShipperOrders(string shipperId, int page = 1)
        {
            var shipper = await _userManager.FindByIdAsync(shipperId);
            if (shipper == null) return NotFound();

            const int pageSize = 15;
            var query = _db.Orders
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .Where(o => o.ShipperId == shipperId)
                .OrderByDescending(o => o.OrderDate);

            var total = await query.CountAsync();
            var orders = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Shipper = shipper;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.TotalOrders = total;
            return View(orders);
        }

        [HttpGet("/api/admin/shippers/locations")]
        public async Task<IActionResult> ActiveLocations()
        {
            var activeOrders = await _db.Orders
                .Include(o => o.Shipper)
                .Where(o => o.DeliveryStatus == "delivering"
                            && o.ShipperLatitude != null
                            && o.ShipperLongitude != null)
                .ToListAsync();

            var result = activeOrders.Select(o => new
            {
                orderId = o.Id,
                shipperId = o.ShipperId,
                shipperName = o.Shipper != null ? $"{o.Shipper.FirstName} {o.Shipper.LastName}".Trim() : "Shipper",
                lat = o.ShipperLatitude,
                lng = o.ShipperLongitude,
                destLat = o.Latitude,
                destLng = o.Longitude,
                address = o.DeliveryAddress,
                updatedAt = o.ShipperLocationUpdatedAt
            });
            return Ok(result);
        }
    }

    public class ShipperStatViewModel
    {
        public string ShipperId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public int TotalCompleted { get; set; }
        public int TodayCompleted { get; set; }
        public Order? ActiveOrder { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
    }
}
