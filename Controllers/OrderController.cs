using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LaPizzaria.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IOrderService _orderService;
        private readonly IQrService _qrService;
        private readonly IComboService _comboService;
        private readonly IVoucherService _voucherService;

        public OrderController(ApplicationDbContext db, IOrderService orderService, IQrService qrService, IComboService comboService, IVoucherService voucherService)
        {
            _db = db;
            _orderService = orderService;
            _qrService = qrService;
            _comboService = comboService;
            _voucherService = voucherService;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _db.Orders.Include(o => o.OrderDetails).ToListAsync();
            return View(orders);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ScanQr()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Qr(string? tableCode)
        {
            ViewBag.TableCode = tableCode ?? string.Empty;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Preview([FromBody] QrOrderRequest req)
        {
            var items = req.Items ?? new List<QrOrderItem>();
            var productIds = items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            var details = new List<OrderDetail>();
            foreach (var it in items)
            {
                if (!products.TryGetValue(it.ProductId, out var p) && it.UnitPrice == null)
                {
                    // Unknown product and no explicit price; skip
                    continue;
                }
                var price = it.UnitPrice ?? (products.TryGetValue(it.ProductId, out var prod) ? prod.Price : 0m);
                details.Add(new OrderDetail { ProductId = it.ProductId, Quantity = it.Quantity, UnitPrice = price, Subtotal = price * it.Quantity });
            }

            var subtotal = details.Sum(d => d.Subtotal);
            // Combo is now selected like normal products in QR; no automatic combo discount
            var discount = 0m;
            // Apply voucher discounts (up to 2)
            var vouchers = new List<Voucher>();
            if (req.VoucherIds != null)
            {
                foreach (var vid in req.VoucherIds.Take(2))
                {
                    var v = await _voucherService.GetByIdAsync(vid);
                    if (v != null && _voucherService.IsUsable(v, System.DateTime.UtcNow)) vouchers.Add(v);
                }
            }
            decimal voucherDiscount = 0m;
            foreach (var v in vouchers)
            {
                voucherDiscount += Math.Round(subtotal * (v.DiscountPercent / 100m), 2);
            }

            var total = Math.Max(0, subtotal - voucherDiscount);
            var vInfo = vouchers.Select(v => new { id = v.Id, code = v.Code, name = v.Name, percent = v.DiscountPercent });
            return Ok(new { subtotal, discount, voucherDiscount, total, vouchers = vInfo });
        }

        [HttpGet]
        public async Task<IActionResult> SplitForm(int orderId)
        {
            var order = await _db.Orders.Include(o => o.OrderDetails).ThenInclude(od => od.Product).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SplitForm(int orderId, List<int> orderDetailId, List<int> moveQty)
        {
            var map = new Dictionary<int, int>();
            for (int i = 0; i < orderDetailId.Count; i++)
            {
                var qty = i < moveQty.Count ? moveQty[i] : 0;
                if (qty > 0) map[orderDetailId[i]] = qty;
            }
            await _orderService.SplitOrderAsync(orderId, map);
            TempData["success"] = "Tách đơn thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Pay(int orderId, string method = "Cash")
        {
            var order = await _db.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound();
            order.TotalPrice = await _orderService.CalculateTotalAsync(order.Id);
            order.PaymentMethod = method;
            order.OrderStatus = "Completed";
            await _db.SaveChangesAsync();
            TempData["success"] = $"Đã thanh toán đơn #{order.Id}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recalculate(int orderId)
        {
            var order = await _db.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound();
            order.TotalPrice = await _orderService.CalculateTotalAsync(order.Id);
            await _db.SaveChangesAsync();
            TempData["success"] = $"Đã tính lại tổng đơn #{order.Id}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
        {
            try
            {
                var details = req.Items.Select(i => new OrderDetail
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.UnitPrice * i.Quantity
                }).ToList();

                var order = await _orderService.CreateOrderAsync(req.UserId, details, req.TableIds ?? new List<int>());
                // Attach up to 2 vouchers if provided and valid
                if (req.VoucherIds != null && req.VoucherIds.Count > 0)
                {
                    var ids = req.VoucherIds.Take(2).ToList();
                    foreach (var vid in ids)
                    {
                        var v = await _voucherService.GetByIdAsync(vid);
                        if (v != null && _voucherService.IsUsable(v, System.DateTime.UtcNow))
                        {
                            _db.OrderVouchers.Add(new OrderVoucher { OrderId = order.Id, VoucherId = v.Id });
                            v.UsedCount += 1;
                        }
                    }
                    await _db.SaveChangesAsync();
                }
                return Ok(order.Id);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MergeTables(int orderId, [FromForm] List<int> tableIds)
        {
            if (orderId <= 0 || tableIds == null || tableIds.Count == 0)
            {
                TempData["error"] = "Vui lòng chọn Order và ít nhất một bàn.";
                return RedirectToAction("Index", "Table");
            }

            // enforce exactly 2 tables
            var distinctIds = tableIds.Distinct().ToList();
            if (distinctIds.Count != 2)
            {
                TempData["error"] = "Vui lòng chọn đúng 2 bàn để gộp.";
                return RedirectToAction("Index", "Table");
            }

            var ok = await _orderService.MergeTablesAsync(orderId, distinctIds);
            if (!ok)
            {
                TempData["error"] = "Không thể gộp: có bàn đang được sử dụng bởi order khác.";
            }
            else
            {
                TempData["success"] = "Gộp bàn thành công.";
            }
            return RedirectToAction("Index", "Table");
        }

        [HttpPost]
        public async Task<IActionResult> Assign(int orderId, int tableId)
        {
            if (orderId <= 0 || tableId <= 0)
            {
                TempData["error"] = "Thiếu Order hoặc Bàn.";
                return RedirectToAction("Index", "Table");
            }
            var ok = await _orderService.AssignTablesAsync(orderId, new List<int> { tableId });
            if (!ok)
            {
                TempData["error"] = "Bàn đang được sử dụng bởi order khác.";
            }
            else
            {
                TempData["success"] = "Đã gắn bàn vào order.";
            }
            return RedirectToAction("Index", "Table");
        }

        [HttpPost]
        public async Task<IActionResult> Split(int orderId, [FromBody] Dictionary<int, int> move)
        {
            var orders = await _orderService.SplitOrderAsync(orderId, move);
            return Ok(orders.Select(o => o.Id));
        }

        [HttpGet]
        public IActionResult GenerateQr(string tableCode)
        {
            var payload = _qrService.GenerateTableQrPayload(tableCode);
            return Ok(payload);
        }

        // Simple QR entry: customer scans QR that encodes table code => front-end posts to /Order/FromQr
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> FromQr([FromBody] QrOrderRequest req)
        {
            try
            {
                var details = new List<OrderDetail>();
                if (req.Items != null)
                {
                    var productIds = req.Items.Select(i => i.ProductId).Distinct().ToList();
                    var priceMap = await _db.Products.Where(p => productIds.Contains(p.Id))
                        .ToDictionaryAsync(p => p.Id, p => p.Price);
                    foreach (var it in req.Items)
                    {
                        decimal price = it.UnitPrice ?? (priceMap.TryGetValue(it.ProductId, out var p) ? p : 0m);
                        details.Add(new OrderDetail
                        {
                            ProductId = it.ProductId,
                            Quantity = it.Quantity,
                            UnitPrice = price,
                            Subtotal = price * it.Quantity
                        });
                    }
                }
                var tableIds = new List<int>();
                if (!string.IsNullOrWhiteSpace(req.TableCode))
                {
                    var t = await _db.Tables.FirstOrDefaultAsync(x => x.Code == req.TableCode);
                    if (t != null) tableIds.Add(t.Id);
                }
                var order = await _orderService.CreateOrderAsync(null, details, tableIds);
                if (req.VoucherIds != null && req.VoucherIds.Count > 0)
                {
                    foreach (var vid in req.VoucherIds.Take(2))
                    {
                        var v = await _voucherService.GetByIdAsync(vid);
                        if (v != null && _voucherService.IsUsable(v, System.DateTime.UtcNow))
                        {
                            _db.OrderVouchers.Add(new OrderVoucher { OrderId = order.Id, VoucherId = v.Id });
                            v.UsedCount += 1;
                        }
                    }
                    await _db.SaveChangesAsync();
                }
                return Ok(new { orderId = order.Id });
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class CreateOrderRequest
    {
        public string? UserId { get; set; }
        public List<ItemDto> Items { get; set; } = new();
        public List<int>? TableIds { get; set; }
        public List<int> VoucherIds { get; set; } = new();
    }

    public class ItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class QrOrderRequest
    {
        public string? TableCode { get; set; }
        public List<QrOrderItem>? Items { get; set; }
        public List<int>? VoucherIds { get; set; }
    }

    public class QrOrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
    }
}


