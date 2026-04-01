using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LaPizzaria.ViewModels;
using LaPizzaria.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace LaPizzaria.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IOrderService _orderService;
        private readonly IQrService _qrService;
        private readonly IComboService _comboService;
        private readonly IVoucherService _voucherService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _memoryCache;
        private readonly IMomoService _momoService;
        private readonly IOrderPlacementService _orderPlacement;

        public OrderController(ApplicationDbContext db, IOrderService orderService, IQrService qrService, IComboService comboService, IVoucherService voucherService, UserManager<ApplicationUser> userManager, IConfiguration config, IMemoryCache memoryCache, IMomoService momoService, IOrderPlacementService orderPlacement)
        {
            _db = db;
            _orderService = orderService;
            _qrService = qrService;
            _comboService = comboService;
            _voucherService = voucherService;
            _userManager = userManager;
            _config = config;
            _memoryCache = memoryCache;
            _momoService = momoService;
            _orderPlacement = orderPlacement;
        }

        public async Task<IActionResult> Index(string? statusFilter, int page = 1)
        {
            if (page < 1) page = 1;
            const int pageSize = 5;
            var userId = _userManager.GetUserId(User);
            var query = _db.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .AsQueryable();
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(o => o.UserId == userId);

            var all = await query.ToListAsync();
            var allCount = all.Count;
            var deliveringCount = all.Count(o => o.OrderStatus == "Delivering" || o.OrderStatus == "Preparing" || o.OrderStatus == "Ready" || o.OrderStatus == "Confirmed" || o.OrderStatus == "Pending");
            var completedCount = all.Count(o => o.OrderStatus == "Completed");
            var cancelledCount = all.Count(o => o.OrderStatus == "Cancelled");

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                if (statusFilter == "Delivering")
                    query = query.Where(o => o.OrderStatus == "Delivering" || o.OrderStatus == "Preparing" || o.OrderStatus == "Ready" || o.OrderStatus == "Confirmed" || o.OrderStatus == "Pending");
                else if (statusFilter == "Rated")
                    query = query.Where(o => false);
                else
                    query = query.Where(o => o.OrderStatus == statusFilter);
            }
            var totalCount = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new OrderHistoryViewModel
            {
                Orders = orders,
                StatusFilter = statusFilter,
                AllCount = allCount,
                DeliveringCount = deliveringCount,
                CompletedCount = completedCount,
                RatedCount = 0,
                CancelledCount = cancelledCount,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
            return View(model);
        }

        /// <summary>Chi tiết đơn giao hàng (shipper) — dữ liệu mock trong view.</summary>
        [HttpGet]
        [Authorize(Roles = "Shipper")]
        public IActionResult Detail(string? id)
        {
            ViewData["Title"] = "Chi tiết đơn hàng";
            ViewBag.OrderCode = string.IsNullOrWhiteSpace(id) ? "ORD-2024" : id;
            return View();
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
            var productIds = items.Select(i => i.ProductId).Concat(items.Where(i => i.ProductId2.HasValue).Select(i => i.ProductId2!.Value)).Distinct().ToList();
            var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            var details = new List<OrderDetail>();
            foreach (var it in items)
            {
                if (!products.TryGetValue(it.ProductId, out var p1) && it.UnitPrice == null)
                {
                    continue;
                }
                
                decimal price = 0;
                if (it.UnitPrice != null)
                {
                    price = it.UnitPrice.Value;
                }
                else if (p1 != null)
                {
                    decimal p1Price = p1.Price;
                    // Apply size modifier
                    decimal modifier = 0;
                    if (it.Size == "S") modifier = -30000;
                    else if (it.Size == "L") modifier = 50000;

                    decimal finalUnitPrice = p1Price + modifier;

                    if (it.ProductId2.HasValue && products.TryGetValue(it.ProductId2.Value, out var p2))
                    {
                        decimal p2Price = p2.Price + modifier;
                        finalUnitPrice = (finalUnitPrice + p2Price) / 2;
                    }
                    price = finalUnitPrice;
                }
                
                details.Add(new OrderDetail { ProductId = it.ProductId, ProductId2 = it.ProductId2, Quantity = it.Quantity, UnitPrice = price, Subtotal = price * it.Quantity, Size = it.Size });
            }

            var subtotal = details.Sum(d => d.Subtotal);
            // Combo is now selected like normal products in QR; no automatic combo discount
            var discount = 0m;
            // Apply voucher discounts (up to 2)
            var vouchers = new List<Voucher>();
            if (req.VoucherIds != null)
            {
                var now = DateTime.Now;
                foreach (var vid in req.VoucherIds.Take(2))
                {
                    var v = await _voucherService.GetByIdAsync(vid);
                    if (v != null && _voucherService.IsUsable(v, now)) vouchers.Add(v);
                }
            }
            decimal voucherDiscount = 0m;
            var validatedVouchers = new List<object>();

            foreach (var vid in req.VoucherIds?.Take(2) ?? new List<int>())
            {
                var v = await _voucherService.GetByIdAsync(vid);
                if (v == null) continue;
                if (!IsVoucherForUser(v, req.UserId))
                {
                    return BadRequest(new { error = $"Voucher {v.Code} không thuộc tài khoản hiện tại." });
                }

                if (!_voucherService.IsUsable(v, DateTime.Now, details))
                {
                    var error = $"Voucher {v.Code} hiện không khả dụng.";
                    if (v.TargetProductId.HasValue && !details.Any(d => d.ProductId == v.TargetProductId.Value || d.ProductId2 == v.TargetProductId.Value))
                        error = $"Mã {v.Code} yêu cầu đơn hàng phải có món ăn chỉ định ({v.TargetProduct?.Name ?? v.TargetProductId.ToString()}).";
                    
                    return BadRequest(new { error });
                }

                if (subtotal < v.MinOrderValue)
                {
                    return BadRequest(new { error = $"Đơn hàng chưa đạt giá trị tối thiểu ({v.MinOrderValue:N0}đ) để sử dụng mã {v.Code}." });
                }

                decimal currentDiscount = _voucherService.CalculateDiscount(v, details, subtotal);
                voucherDiscount += currentDiscount;
                
                validatedVouchers.Add(new { id = v.Id, code = v.Code, name = v.Name, percent = v.DiscountPercent, amount = v.DiscountAmount, type = v.VoucherType, actualDiscount = currentDiscount });
            }

            var total = Math.Max(0, subtotal - voucherDiscount);
            return Ok(new { subtotal, discount, voucherDiscount, total, vouchers = validatedVouchers });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails!)
                .ThenInclude(od => od.Product2)
                .Include(o => o.User)
                .Include(o => o.OrderVouchers!)
                .ThenInclude(ov => ov.Voucher)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        /// <summary>Trang in hóa đơn theo mẫu LP/26E (chỉ nội dung in, không layout).</summary>
        [HttpGet]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderDetails!).ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails!).ThenInclude(od => od.Product2)
                .Include(o => o.User)
                .Include(o => o.OrderVouchers!).ThenInclude(ov => ov.Voucher)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            var subtotal = order.OrderDetails?.Sum(d => d.Subtotal) ?? 0;
            var voucherDiscount = 0m;
            if (order.OrderVouchers != null)
            {
                foreach (var ov in order.OrderVouchers.Take(5))
                {
                    var v = ov.Voucher;
                    if (v != null && v.VoucherType == "Percentage")
                        voucherDiscount += Math.Round(subtotal * (v.DiscountPercent / 100m), 0);
                    else if (v != null && v.VoucherType == "FixedAmount")
                        voucherDiscount += v.DiscountAmount;
                }
            }
            var deliveryFee = 20000m;
            var totalBeforeTax = subtotal + deliveryFee - voucherDiscount;
            if (totalBeforeTax < 0) totalBeforeTax = 0;
            var taxPercent = 10m;
            var taxAmount = Math.Round(totalBeforeTax * (taxPercent / 100m), 0);
            var grandTotal = totalBeforeTax + taxAmount;

            var buyerName = "—";
            var buyerPhone = "—";
            var buyerAddress = order.DeliveryAddress ?? "—";
            if (order.User != null)
            {
                buyerName = $"{order.User.FirstName} {order.User.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(buyerName)) buyerName = order.User.UserName ?? order.User.Email ?? "Khách hàng";
                buyerPhone = order.User.PhoneNumber ?? "—";
                if (string.IsNullOrWhiteSpace(buyerAddress) || buyerAddress == "—") buyerAddress = order.User.Address ?? "—";
            }

            var yearSuffix = DateTime.Now.ToString("yy");
            var vm = new InvoicePrintViewModel
            {
                Order = order,
                InvoiceSymbol = _config["Invoice:Symbol"] ?? "LP/26E",
                InvoiceNumber = (order.Id).ToString("D6"),
                IssueDate = order.OrderDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                SellerName = _config["Invoice:SellerName"] ?? "Công ty TNHH LaPizzaria",
                SellerTaxCode = _config["Invoice:SellerTaxCode"] ?? "0312345678",
                SellerAddress = _config["Invoice:SellerAddress"] ?? "193 Đỗ Văn Thi, Phường, Biên Hòa, Đồng Nai",
                SellerPhone = _config["Invoice:SellerPhone"] ?? "0901 234 567",
                SellerEmail = _config["Invoice:SellerEmail"] ?? "support@lapizzaria.vn",
                BuyerName = buyerName,
                BuyerPhone = buyerPhone,
                BuyerAddress = buyerAddress,
                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                VoucherDiscount = voucherDiscount,
                TotalBeforeTax = totalBeforeTax,
                TaxPercent = taxPercent,
                TaxAmount = taxAmount,
                GrandTotal = grandTotal,
                AmountInWords = NumberToWordsVietnamese.ToWords(grandTotal),
                PaymentMethodDisplay = string.IsNullOrEmpty(order.PaymentMethod) ? "Chưa chọn" : order.PaymentMethod,
                PaymentStatus = "Chưa thanh toán",
                OrderCode = "PH" + order.Id,
                OrderDateDisplay = order.OrderDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                OrderStatusDisplay = order.OrderStatus ?? "Đang chuẩn bị",
                Lines = order.OrderDetails?.Select((od, i) => {
                    var name = od.Product?.Name ?? "Món #" + od.ProductId;
                    if (od.ProductId2.HasValue && od.Product2 != null)
                    {
                        name = $"Mix: {od.Product?.Name} / {od.Product2.Name}";
                    }
                    if (!string.IsNullOrEmpty(od.Size))
                    {
                        name += $" (Size {od.Size})";
                    }

                    return new InvoicePrintLine
                    {
                        Stt = i + 1,
                        ProductName = name,
                        Quantity = od.Quantity,
                        UnitPrice = od.UnitPrice
                    };
                }).ToList() ?? new List<InvoicePrintLine>()
            };

            return View(vm);
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
                var details = new List<OrderDetail>();
                foreach (var item in req.Items)
                {
                    var p1 = await _db.Products.FindAsync(item.ProductId);
                    if (p1 == null) continue;

                    decimal p1Price = p1.Price;
                    // Apply size modifier
                    decimal modifier = 0;
                    if (item.Size == "S") modifier = -30000;
                    else if (item.Size == "L") modifier = 50000;

                    decimal finalUnitPrice = p1Price + modifier;

                    if (item.ProductId2.HasValue)
                    {
                        var p2 = await _db.Products.FindAsync(item.ProductId2.Value);
                        if (p2 != null)
                        {
                            decimal p2Price = p2.Price + modifier;
                            finalUnitPrice = (finalUnitPrice + p2Price) / 2;
                        }
                    }

                    details.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        ProductId2 = item.ProductId2,
                        Quantity = item.Quantity,
                        UnitPrice = finalUnitPrice,
                        Subtotal = finalUnitPrice * item.Quantity,
                        Size = item.Size
                    });
                }

                var order = await _orderService.CreateOrderAsync(req.UserId, details, req.TableIds ?? new List<int>(), req.DeliveryAddress, req.Latitude, req.Longitude);
                // Attach up to 2 vouchers if provided and valid
                if (req.VoucherIds != null && req.VoucherIds.Count > 0)
                {
                    var ids = req.VoucherIds.Take(2).ToList();
                    var currentUserId = _userManager.GetUserId(User);
                    var effectiveUserId = !string.IsNullOrWhiteSpace(currentUserId) ? currentUserId : req.UserId;
                    foreach (var vid in ids)
                    {
                        if (v != null && IsVoucherForUser(v, effectiveUserId) && _voucherService.IsUsable(v, DateTime.Now, details))
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
<<<<<<< HEAD
                var orderId = await _orderPlacement.PlaceQrOrderAsync(req, "Cash");
                return Ok(new { orderId });
=======
                var details = new List<OrderDetail>();
                if (req.Items != null)
                {
                    var productIds = req.Items.Select(i => i.ProductId).Distinct().ToList();
                    var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

                    foreach (var it in req.Items)
                    {
                        decimal price = it.UnitPrice ?? 0m; // Default to 0 if UnitPrice is null
                        if (it.UnitPrice == null && products.TryGetValue(it.ProductId, out var p1))
                        {
                            decimal p1Price = p1.Price;
                            // Apply size modifier
                            decimal modifier = 0;
                            if (it.Size == "S") modifier = -30000;
                            else if (it.Size == "L") modifier = 50000;

                            decimal finalUnitPrice = p1Price + modifier;

                            if (it.ProductId2.HasValue && products.TryGetValue(it.ProductId2.Value, out var p2))
                            {
                                decimal p2Price = p2.Price + modifier;
                                finalUnitPrice = (finalUnitPrice + p2Price) / 2;
                            }
                            price = finalUnitPrice;
                        }

                        details.Add(new OrderDetail
                        {
                            ProductId = it.ProductId,
                            ProductId2 = it.ProductId2,
                            Quantity = it.Quantity,
                            UnitPrice = price,
                            Subtotal = price * it.Quantity,
                            Size = it.Size
                        });
                    }
                }
                var tableIds = new List<int>();
                if (!string.IsNullOrWhiteSpace(req.TableCode))
                {
                    var t = await _db.Tables.FirstOrDefaultAsync(x => x.Code == req.TableCode);
                    if (t != null) tableIds.Add(t.Id);
                }
                var order = await _orderService.CreateOrderAsync(req.UserId, details, tableIds, req.DeliveryAddress, req.Latitude, req.Longitude);
                if (req.VoucherIds != null && req.VoucherIds.Count > 0)
                {
                    foreach (var vid in req.VoucherIds.Take(2))
                    {
                        var v = await _voucherService.GetByIdAsync(vid);
                        if (v != null && _voucherService.IsUsable(v, System.DateTime.Now, details))
                        {
                            _db.OrderVouchers.Add(new OrderVoucher { OrderId = order.Id, VoucherId = v.Id });
                            v.UsedCount += 1;
                        }
                    }
                    await _db.SaveChangesAsync();
                }
                return Ok(new { orderId = order.Id });
>>>>>>> e04f25f (Voucher)
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Chuẩn bị thanh toán MoMo: lưu giỏ tạm, trả về payUrl (không tạo đơn).</summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PrepareMoMoPayment([FromBody] QrOrderRequest req, CancellationToken cancellationToken)
        {
            var deliveryType = string.IsNullOrWhiteSpace(req.DeliveryType) ? "ship" : req.DeliveryType;
            if (string.Equals(deliveryType, "ship", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(req.DeliveryAddress))
                    return BadRequest(new { error = "Vui lòng nhập địa chỉ giao hàng." });
                if (req.TravelDistanceMeters.HasValue && req.TravelDistanceMeters.Value < 0)
                    return BadRequest(new { error = "Địa chỉ nằm ngoài phạm vi giao hàng." });
            }

            var outcome = await _orderPlacement.ComputeTotalsAsync(req);
            if (!outcome.Success || outcome.Totals == null)
                return BadRequest(new { error = outcome.Error ?? "Không tính được tổng tiền." });

            var pendingId = Guid.NewGuid();
            var cacheKey = $"momo_pending_{pendingId}";
            _memoryCache.Set(cacheKey, new PendingCheckoutState
            {
                Request = req,
                ExpectedAmountVnd = outcome.Totals.AmountVnd
            }, TimeSpan.FromMinutes(30));

            var apiResult = await _momoService.CreatePaymentAsync(new OrderInfoModel
            {
                OrderId = pendingId.ToString("N")[..12],
                Name = "Thanh toan don hang LaPizzaria",
                Amount = outcome.Totals.GrandTotal,
                ExtraData = pendingId.ToString()
            }, cancellationToken);

            if (apiResult == null || apiResult.ResultCode != 0 || string.IsNullOrWhiteSpace(apiResult.PayUrl))
            {
                _memoryCache.Remove(cacheKey);
                return BadRequest(new { error = apiResult?.Message ?? "Không tạo được giao dịch MoMo. Kiểm tra cấu hình." });
            }

            return Ok(new { payUrl = apiResult.PayUrl });
        }

        private static bool IsVoucherForUser(Voucher voucher, string? userId)
        {
            if (string.IsNullOrWhiteSpace(voucher.TargetUserId)) return true; // public voucher
            if (string.IsNullOrWhiteSpace(userId)) return false;
            return string.Equals(voucher.TargetUserId, userId, StringComparison.Ordinal);
        }
    }

    public class CreateOrderRequest
    {
        public string? UserId { get; set; }
        public string? DeliveryAddress { get; set; }
        public List<ItemDto> Items { get; set; } = new();
        public List<int>? TableIds { get; set; }
        public List<int> VoucherIds { get; set; } = new();
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class ItemDto
    {
        public int ProductId { get; set; }
        public int? ProductId2 { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Size { get; set; }
    }
}
