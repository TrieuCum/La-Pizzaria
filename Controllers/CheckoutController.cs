using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.Services;
using LaPizzaria.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOrderService _orderService;
        private readonly IVoucherService _voucherService;
        private readonly ILoyaltyService _loyaltyService;

        public CheckoutController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IOrderService orderService, IVoucherService voucherService, ILoyaltyService loyaltyService)
        {
            _db = db;
            _userManager = userManager;
            _orderService = orderService;
            _voucherService = voucherService;
            _loyaltyService = loyaltyService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Get cart items from database (or session)
            var cartItems = await _db.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == user.Id)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Home");
            }

            // Get dessert suggestions
            var desserts = await _db.Products
                .Where(p => p.IsActive && (p.Category.Contains("Tráng miệng") || p.Category.Contains("Dessert")))
                .Take(6)
                .ToListAsync();

            ViewBag.CartItems = cartItems;
            ViewBag.Desserts = desserts;
            ViewBag.User = user;

            var viewModel = new LaPizzaria.ViewModels.CheckoutViewModel
            {
                DeliveryType = "DineIn",
                PaymentMethod = "Cash"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(LaPizzaria.ViewModels.CheckoutViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var cartItems = await _db.CartItems
                    .Include(ci => ci.Product)
                    .Where(ci => ci.UserId == user.Id)
                    .ToListAsync();
                var desserts = await _db.Products
                    .Where(p => p.IsActive && (p.Category.Contains("Tráng miệng") || p.Category.Contains("Dessert")))
                    .Take(6)
                    .ToListAsync();
                ViewBag.CartItems = cartItems;
                ViewBag.Desserts = desserts;
                ViewBag.User = user;
                return View("Index", model);
            }

            // Get cart items
            var items = await _db.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == user.Id)
                .ToListAsync();

            if (!items.Any())
            {
                TempData["error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index");
            }

            // Create order details
            var orderDetails = items.Select(ci => new OrderDetail
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = ci.Product.Price,
                Subtotal = ci.Product.Price * ci.Quantity
            }).ToList();

            // Create order
            var order = new Order
            {
                UserId = user.Id,
                OrderCode = GenerateOrderCode(),
                OrderStatus = OrderStatus.Pending,
                DeliveryType = model.DeliveryType,
                DeliveryAddress = model.DeliveryType == "Delivery" ? model.DeliveryAddress : null,
                Notes = model.Notes,
                PaymentMethod = model.PaymentMethod,
                OrderDetails = orderDetails,
                OrderDate = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Calculate totals
            order.Subtotal = orderDetails.Sum(od => od.Subtotal);
            order.ShippingFee = model.DeliveryType == "Delivery" ? 20000 : 0; // 20k shipping fee
            order.DiscountAmount = 0;

            // Apply vouchers if provided
            if (!string.IsNullOrEmpty(model.VoucherCode))
            {
                var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.Code == model.VoucherCode && v.IsActive);
                if (voucher != null && _voucherService.IsUsable(voucher, DateTime.UtcNow))
                {
                    order.DiscountAmount = Math.Round(order.Subtotal * (voucher.DiscountPercent / 100m), 2);
                    _db.OrderVouchers.Add(new OrderVoucher { OrderId = order.Id, VoucherId = voucher.Id });
                    voucher.UsedCount++;
                }
            }

            order.TotalPrice = order.Subtotal + order.ShippingFee - order.DiscountAmount;
            order.EstimatedDeliveryTime = model.DeliveryType == "Delivery" 
                ? DateTime.UtcNow.AddMinutes(45) 
                : DateTime.UtcNow.AddMinutes(20);

            await _db.SaveChangesAsync();

            // Clear cart
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();

            // Award loyalty points if member
            if (user.IsMember && _loyaltyService != null)
            {
                var points = await _loyaltyService.CalculatePointsAsync(order.TotalPrice);
                if (points > 0)
                {
                    await _loyaltyService.AwardPointsAsync(user.Id, points, $"Đơn hàng #{order.OrderCode}");
                }
            }

            TempData["success"] = $"Đặt hàng thành công! Mã đơn: #{order.OrderCode}";
            return RedirectToAction("Track", "Order", new { id = order.Id });
        }

        private string GenerateOrderCode()
        {
            var date = DateTime.UtcNow;
            var datePart = date.ToString("yyMMdd");
            var randomPart = new Random().Next(1000, 9999).ToString();
            return $"LP{datePart}{randomPart}";
        }
    }

    public class CheckoutViewModel
    {
        public string DeliveryType { get; set; } = "DineIn";
        public string? DeliveryAddress { get; set; }
        public string? Notes { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? VoucherCode { get; set; }
    }
}
