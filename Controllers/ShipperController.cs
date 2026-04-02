using System.Globalization;
using LaPizzaria.Data;
using LaPizzaria.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Shipper")]
    public class ShipperController : Controller
    {
        private readonly ApplicationDbContext _db;
        private static readonly string[] KitchenReadyStatuses = { "Ready", "Sẵn sàng" };
        private static readonly string[] ActiveDeliveryStatuses = { "assigned", "delivering" };
        private static readonly string[] HistoryDeliveryStatuses = { "delivered", "failed" };

        public ShipperController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["ShipperNav"] = "Index";
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
            {
                TempData["error"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction("Login", "Account");
            }

            var baseQuery = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => !string.IsNullOrWhiteSpace(o.DeliveryAddress));

            var activeOrders = await baseQuery
                .Where(o => o.ShipperId == shipperId && ActiveDeliveryStatuses.Contains(o.DeliveryStatus ?? ""))
                .OrderByDescending(o => o.OrderDate)
                .Take(1)
                .ToListAsync();

            var newOrders = await baseQuery
                .Where(o =>
                    o.ShipperId == null &&
                    KitchenReadyStatuses.Contains(o.OrderStatus))
                .OrderBy(o => o.OrderDate)
                .Take(8)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var todaysDelivered = await _db.Orders
                .Where(o => o.ShipperId == shipperId
                    && o.DeliveryStatus == "delivered"
                    && !string.IsNullOrWhiteSpace(o.DeliveryAddress)
                    && o.DeliveredAt.HasValue
                    && o.DeliveredAt.Value.Date == today)
                .ToListAsync();

            var vm = new ShipperIndexViewModel
            {
                ActiveOrder = activeOrders.Select(MapToSummary).FirstOrDefault(),
                NewOrders = newOrders.Select(MapToSummary).ToList(),
                DeliveredTodayCount = todaysDelivered.Count,
                IncomeToday = todaysDelivered.Sum(GetShipperIncomeForOrder)
            };

            return View(vm);
        }

        public async Task<IActionResult> History(string status = "All", string? fromDate = null, string? toDate = null, int page = 1)
        {
            ViewData["ShipperNav"] = "History";
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
            {
                TempData["error"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction("Login", "Account");
            }
            if (page < 1) page = 1;
            const int pageSize = 9;
            var parsedFromDate = ParseDateDdMmYyyy(fromDate, out var fromDateInvalid);
            var parsedToDate = ParseDateDdMmYyyy(toDate, out var toDateInvalid);
            if (fromDateInvalid || toDateInvalid)
            {
                ViewData["DateFilterError"] = "Ngày lọc không hợp lệ. Vui lòng nhập theo định dạng dd/MM/yyyy.";
            }

            // Keep filter usable even when user enters reversed date range.
            if (parsedFromDate.HasValue && parsedToDate.HasValue && parsedFromDate > parsedToDate)
            {
                (parsedFromDate, parsedToDate) = (parsedToDate, parsedFromDate);
            }

            var query = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .Where(o => o.ShipperId == shipperId
                    && HistoryDeliveryStatuses.Contains(o.DeliveryStatus ?? "")
                    && !string.IsNullOrWhiteSpace(o.DeliveryAddress));

            if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(o => o.DeliveryStatus == status);
            }

            if (parsedFromDate.HasValue)
            {
                var from = parsedFromDate.Value.Date;
                query = query.Where(o => (o.DeliveredAt ?? o.UpdatedAt) >= from);
            }

            if (parsedToDate.HasValue)
            {
                var toExclusive = parsedToDate.Value.Date.AddDays(1);
                query = query.Where(o => (o.DeliveredAt ?? o.UpdatedAt) < toExclusive);
            }

            var totalCount = await query.CountAsync();
            var list = await query
                .OrderByDescending(o => o.DeliveredAt ?? o.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var completedForIncome = await query
                .Where(o => o.DeliveryStatus == "delivered")
                .ToListAsync();
            var income = completedForIncome.Sum(GetShipperIncomeForOrder);

            var vm = new ShipperHistoryViewModel
            {
                Orders = list.Select(MapToSummary).ToList(),
                Status = status,
                FromDate = parsedFromDate,
                ToDate = parsedToDate,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalIncome = income
            };

            return View(vm);
        }

        public async Task<IActionResult> Income(string? orderCodeSearch = null, int page = 1)
        {
            ViewData["ShipperNav"] = "Income";
            if (page < 1) page = 1;
            const int pageSize = 6;

            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-((7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7));
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var sevenDaysStart = today.AddDays(-6);

            var completedOrders = await _db.Orders
                .Where(o => o.OrderStatus == "Completed" && !string.IsNullOrWhiteSpace(o.DeliveryAddress))
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync();

            var incomeToday = completedOrders
                .Where(o => o.UpdatedAt.Date == today)
                .Sum(GetShipperIncomeForOrder);

            var incomeThisWeek = completedOrders
                .Where(o => o.UpdatedAt.Date >= weekStart && o.UpdatedAt.Date <= today)
                .Sum(GetShipperIncomeForOrder);

            var monthOrders = completedOrders
                .Where(o => o.UpdatedAt.Date >= monthStart && o.UpdatedAt.Date <= today)
                .ToList();

            var incomeThisMonth = monthOrders.Sum(GetShipperIncomeForOrder);
            var completedOrdersThisMonth = monthOrders.Count;

            var incomeByDay = completedOrders
                .Where(o => o.UpdatedAt.Date >= sevenDaysStart && o.UpdatedAt.Date <= today)
                .GroupBy(o => o.UpdatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Sum(GetShipperIncomeForOrder));

            var dayLabels = CultureInfo.GetCultureInfo("vi-VN").DateTimeFormat.AbbreviatedDayNames;
            var chartValues = new List<IncomeChartPointViewModel>();
            for (var i = 0; i < 7; i++)
            {
                var day = sevenDaysStart.AddDays(i);
                var value = incomeByDay.TryGetValue(day, out var amount) ? amount : 0m;
                var label = dayLabels[(int)day.DayOfWeek];
                chartValues.Add(new IncomeChartPointViewModel
                {
                    Label = label,
                    Value = value,
                    Highlight = day == today
                });
            }

            var maxDataValue = chartValues.Any() ? chartValues.Max(x => x.Value) : 0m;
            var maxChartValue = Math.Max(2000000m, Math.Ceiling(maxDataValue / 500000m) * 500000m);
            if (maxChartValue <= 0) maxChartValue = 2000000m;

            foreach (var point in chartValues)
            {
                point.HeightPercent = (int)Math.Round((point.Value / maxChartValue) * 100m);
                if (point.HeightPercent > 0 && point.HeightPercent < 8)
                {
                    point.HeightPercent = 8;
                }
            }

            var filteredOrders = completedOrders.AsQueryable();
            if (!string.IsNullOrWhiteSpace(orderCodeSearch))
            {
                var keyword = orderCodeSearch.Trim();
                filteredOrders = filteredOrders.Where(o => o.Id.ToString().Contains(keyword));
            }

            var totalCount = filteredOrders.Count();
            var recentRows = filteredOrders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new ShipperIncomeOrderRowViewModel
                {
                    OrderId = o.Id,
                    CompletedAt = o.UpdatedAt,
                    ShippingFee = GetShipperIncomeForOrder(o),
                    TipAmount = 0m,
                    TotalIncome = GetShipperIncomeForOrder(o),
                    DistanceText = "N/A",
                    StatusLabel = o.OrderStatus == "Completed" ? "Da nhan" : "Cho duyet",
                    IsCompleted = o.OrderStatus == "Completed"
                })
                .ToList();

            var vm = new ShipperIncomeViewModel
            {
                IncomeToday = incomeToday,
                IncomeThisWeek = incomeThisWeek,
                IncomeThisMonth = incomeThisMonth,
                CompletedOrdersThisMonth = completedOrdersThisMonth,
                TotalLast7Days = chartValues.Sum(x => x.Value),
                MaxChartValue = maxChartValue,
                OrderCodeSearch = orderCodeSearch?.Trim() ?? string.Empty,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Last7Days = chartValues,
                RecentCompletedOrders = recentRows
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> MonthlySummary(int? month = null, int? year = null)
        {
            ViewData["ShipperNav"] = "Income";

            var now = DateTime.UtcNow;
            var selectedMonth = month.GetValueOrDefault(now.Month);
            var selectedYear = year.GetValueOrDefault(now.Year);
            if (selectedMonth < 1 || selectedMonth > 12) selectedMonth = now.Month;
            if (selectedYear < 2000 || selectedYear > 2100) selectedYear = now.Year;

            var start = new DateTime(selectedYear, selectedMonth, 1);
            var end = start.AddMonths(1);

            var monthOrders = await _db.Orders
                .Where(o => o.OrderStatus == "Completed"
                    && !string.IsNullOrWhiteSpace(o.DeliveryAddress)
                    && o.UpdatedAt >= start && o.UpdatedAt < end)
                .OrderByDescending(o => o.UpdatedAt)
                .ToListAsync();

            var groupByDay = monthOrders
                .GroupBy(o => o.UpdatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Sum(GetShipperIncomeForOrder));

            var chart = new List<IncomeChartPointViewModel>();
            var daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);
            for (var day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(selectedYear, selectedMonth, day);
                var value = groupByDay.TryGetValue(date.Date, out var amount) ? amount : 0m;
                chart.Add(new IncomeChartPointViewModel
                {
                    Label = day.ToString(),
                    Value = value
                });
            }

            var maxData = chart.Any() ? chart.Max(x => x.Value) : 0m;
            var maxChartValue = Math.Max(2000000m, Math.Ceiling(maxData / 500000m) * 500000m);
            if (maxChartValue <= 0) maxChartValue = 2000000m;
            foreach (var point in chart)
            {
                point.HeightPercent = (int)Math.Round((point.Value / maxChartValue) * 100m);
                if (point.HeightPercent > 0 && point.HeightPercent < 6) point.HeightPercent = 6;
            }

            var vm = new ShipperMonthlySummaryViewModel
            {
                Month = selectedMonth,
                Year = selectedYear,
                TotalIncome = monthOrders.Sum(GetShipperIncomeForOrder),
                TotalOrders = monthOrders.Count,
                MaxChartValue = maxChartValue,
                DailyChart = chart,
                Orders = monthOrders.Select(o => new ShipperIncomeOrderRowViewModel
                {
                    OrderId = o.Id,
                    CompletedAt = o.UpdatedAt,
                    ShippingFee = GetShipperIncomeForOrder(o),
                    TipAmount = 0m,
                    TotalIncome = GetShipperIncomeForOrder(o),
                    DistanceText = "N/A",
                    IsCompleted = true,
                    StatusLabel = "Đã nhận"
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Withdraw()
        {
            ViewData["ShipperNav"] = "Income";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = !string.IsNullOrWhiteSpace(userId)
                ? await _db.Users.FirstOrDefaultAsync(u => u.Id == userId)
                : null;

            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var completedMonthOrders = await _db.Orders
                .Where(o => o.OrderStatus == "Completed"
                    && !string.IsNullOrWhiteSpace(o.DeliveryAddress)
                    && o.UpdatedAt >= monthStart && o.UpdatedAt <= today.AddDays(1))
                .ToListAsync();
            var availableAmount = completedMonthOrders.Sum(GetShipperIncomeForOrder);

            var fullName = $"{user?.FirstName} {user?.LastName}".Trim();
            var vm = new ShipperWithdrawViewModel
            {
                AccountName = string.IsNullOrWhiteSpace(fullName) ? (user?.UserName ?? "Shipper") : fullName,
                BankName = "Vietcombank",
                BankAccountNumber = "Chưa cập nhật",
                AvailableAmount = availableAmount
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult WithdrawConfirm()
        {
            TempData["WithdrawSuccess"] = "1";
            return RedirectToAction(nameof(Withdraw));
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
            {
                TempData["error"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction("Login", "Account");
            }

            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || string.IsNullOrWhiteSpace(order.DeliveryAddress))
                return NotFound();

            var canViewOrder = order.ShipperId == null || order.ShipperId == shipperId;
            if (!canViewOrder)
            {
                TempData["error"] = "Bạn không có quyền truy cập đơn này.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new ShipperOrderDetailViewModel
            {
                OrderId = order.Id,
                OrderStatus = order.OrderStatus,
                DeliveryStatus = order.DeliveryStatus ?? string.Empty,
                CustomerName = GetCustomerName(order),
                CustomerPhone = order.User?.PhoneNumber,
                DeliveryAddress = order.DeliveryAddress ?? string.Empty,
                Note = order.Notes,
                PaymentMethod = order.PaymentMethod,
                OrderDate = order.OrderDate,
                TotalPrice = order.TotalPrice,
                AssignedAt = order.AssignedAt,
                DeliveredAt = order.DeliveredAt,
                Latitude = order.Latitude,
                Longitude = order.Longitude,
                ShipperLatitude = order.ShipperLatitude,
                ShipperLongitude = order.ShipperLongitude,
                ShipperLocationUpdatedAt = order.ShipperLocationUpdatedAt,
                ShipperLocationIp = order.ShipperLocationIp,
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
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(shipperId))
            {
                TempData["error"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE Orders
SET OrderStatus = {"Delivering"},
    ShipperId = {shipperId},
    DeliveryStatus = {"assigned"},
    AssignedAt = {DateTime.UtcNow},
    DeliveredAt = {null},
    UpdatedAt = {DateTime.UtcNow}
WHERE Id = {id}
  AND ShipperId IS NULL
  AND (OrderStatus = {"Ready"} OR OrderStatus = {"Sẵn sàng"})
");

            if (affectedRows == 0)
            {
                TempData["error"] = "Đơn đã được shipper khác nhận";
                return RedirectToAction(nameof(Index));
            }

            TempData["success"] = "Nhận đơn thành công. Đơn đã chuyển sang trạng thái Đang giao.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Shipper")]
        [Route("api/shipper/orders/{id:int}/accept")]
        [Route("orders/{id:int}/accept")]
        public async Task<IActionResult> AcceptApi(int id)
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(shipperId))
                return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ." });

            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE Orders
SET OrderStatus = {"Delivering"},
    ShipperId = {shipperId},
    DeliveryStatus = {"assigned"},
    AssignedAt = {DateTime.UtcNow},
    DeliveredAt = {null},
    UpdatedAt = {DateTime.UtcNow}
WHERE Id = {id}
  AND ShipperId IS NULL
  AND (OrderStatus = {"Ready"} OR OrderStatus = {"Sẵn sàng"})
");

            if (affectedRows == 0)
                return Conflict(new { success = false, message = "Đơn đã được shipper khác nhận" });

            return Ok(new { success = true, message = "Nhận đơn thành công", orderStatus = "Delivering", deliveryStatus = "assigned" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartDelivery(int id, double? latitude, double? longitude)
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
            {
                TempData["error"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            if (!latitude.HasValue || !longitude.HasValue
                || latitude.Value < -90 || latitude.Value > 90
                || longitude.Value < -180 || longitude.Value > 180)
            {
                TempData["error"] = "Bạn phải bật định vị và cho phép trình duyệt truy cập vị trí trước khi bắt đầu giao hàng.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var now = DateTime.UtcNow;
            var clientIp = GetClientIp(HttpContext);
            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE Orders
SET DeliveryStatus = {"delivering"},
    ShipperLatitude = {latitude},
    ShipperLongitude = {longitude},
    ShipperLocationUpdatedAt = {now},
    ShipperLocationIp = {clientIp},
    UpdatedAt = {now}
WHERE Id = {id}
  AND ShipperId = {shipperId}
  AND DeliveryStatus = {"assigned"};
");

            TempData[affectedRows == 1 ? "success" : "error"] =
                affectedRows == 1 ? "Đơn đã chuyển sang trạng thái đang giao." : "Không thể cập nhật trạng thái đơn.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
            {
                TempData["error"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.UtcNow;
            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE Orders
SET DeliveryStatus = {"delivered"},
    OrderStatus = {"Completed"},
    DeliveredAt = {now},
    UpdatedAt = {now}
WHERE Id = {id}
  AND ShipperId = {shipperId}
  AND DeliveryStatus = {"delivering"};
");

            TempData[affectedRows == 1 ? "success" : "error"] =
                affectedRows == 1 ? "Đã xác nhận giao thành công." : "Không thể hoàn tất đơn. Hãy kiểm tra lại trạng thái.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fail(int id)
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
            {
                TempData["error"] = "Phiên đăng nhập không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.UtcNow;
            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE Orders
SET DeliveryStatus = {"failed"},
    OrderStatus = {"Cancelled"},
    DeliveredAt = {now},
    UpdatedAt = {now}
WHERE Id = {id}
  AND ShipperId = {shipperId}
  AND (DeliveryStatus = {"assigned"} OR DeliveryStatus = {"delivering"});
");

            TempData[affectedRows == 1 ? "success" : "error"] =
                affectedRows == 1 ? "Đã cập nhật đơn thành giao thất bại." : "Không thể cập nhật giao thất bại.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpGet]
        [Route("orders/available")]
        public async Task<IActionResult> AvailableOrders(int page = 1, int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .Where(o => o.ShipperId == null
                    && KitchenReadyStatuses.Contains(o.OrderStatus)
                    && !string.IsNullOrWhiteSpace(o.DeliveryAddress));

            var totalCount = await query.CountAsync();
            var orderEntities = await query.OrderBy(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var orders = orderEntities.Select(MapToSummary).ToList();

            return Ok(new { page, pageSize, totalCount, items = orders });
        }

        [HttpGet]
        [Route("orders/my")]
        public async Task<IActionResult> MyOrders()
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
                return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ." });

            var orderEntities = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .Where(o => o.ShipperId == shipperId && ActiveDeliveryStatuses.Contains(o.DeliveryStatus ?? ""))
                .OrderByDescending(o => o.AssignedAt ?? o.UpdatedAt)
                .ToListAsync();
            var orders = orderEntities.Select(MapToSummary).ToList();

            return Ok(new { success = true, items = orders });
        }

        [HttpGet]
        [Route("orders/history")]
        public async Task<IActionResult> DeliveryHistory(int page = 1, int pageSize = 10)
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
                return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ." });

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .Where(o => o.ShipperId == shipperId && HistoryDeliveryStatuses.Contains(o.DeliveryStatus ?? ""));

            var totalCount = await query.CountAsync();
            var orderEntities = await query.OrderByDescending(o => o.DeliveredAt ?? o.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var orders = orderEntities.Select(o => new
            {
                summary = MapToSummary(o),
                o.AssignedAt,
                o.DeliveredAt,
                o.DeliveryStatus
            }).ToList();

            return Ok(new { success = true, page, pageSize, totalCount, items = orders });
        }

        [HttpPut]
        [Route("orders/{id:int}/status")]
        public async Task<IActionResult> UpdateDeliveryStatusApi(int id, [FromBody] UpdateDeliveryStatusRequest request)
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
                return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ." });
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
                return BadRequest(new { success = false, message = "Thiếu trạng thái cần cập nhật." });

            var status = request.Status.Trim().ToLowerInvariant();
            if (status != "delivering" && status != "delivered" && status != "failed")
                return BadRequest(new { success = false, message = "Trạng thái không hợp lệ." });

            var now = DateTime.UtcNow;
            int affectedRows;
            if (status == "delivering")
            {
                if (!request.Latitude.HasValue || !request.Longitude.HasValue
                    || request.Latitude.Value < -90 || request.Latitude.Value > 90
                    || request.Longitude.Value < -180 || request.Longitude.Value > 180)
                {
                    return BadRequest(new { success = false, message = "Phải gửi tọa độ GPS hợp lệ (latitude, longitude) khi chuyển sang đang giao." });
                }

                var lat = request.Latitude.Value;
                var lng = request.Longitude.Value;
                var clientIp = GetClientIp(HttpContext);
                affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE Orders
SET DeliveryStatus = {"delivering"},
    ShipperLatitude = {lat},
    ShipperLongitude = {lng},
    ShipperLocationUpdatedAt = {now},
    ShipperLocationIp = {clientIp},
    UpdatedAt = {now}
WHERE Id = {id}
  AND ShipperId = {shipperId}
  AND DeliveryStatus = {"assigned"};
");
            }
            else if (status == "delivered")
            {
                affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE Orders
SET DeliveryStatus = {"delivered"},
    OrderStatus = {"Completed"},
    DeliveredAt = {now},
    UpdatedAt = {now}
WHERE Id = {id}
  AND ShipperId = {shipperId}
  AND DeliveryStatus = {"delivering"};
");
            }
            else
            {
                affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE Orders
SET DeliveryStatus = {"failed"},
    OrderStatus = {"Cancelled"},
    DeliveredAt = {now},
    UpdatedAt = {now}
WHERE Id = {id}
  AND ShipperId = {shipperId}
  AND (DeliveryStatus = {"assigned"} OR DeliveryStatus = {"delivering"});
");
            }

            if (affectedRows == 0)
                return Conflict(new { success = false, message = "Không thể cập nhật do sai luồng trạng thái hoặc đơn không thuộc shipper hiện tại." });

            return Ok(new { success = true, message = "Cập nhật trạng thái thành công.", status });
        }

        [HttpPost]
        [Route("api/shipper/orders/{id:int}/location")]
        public async Task<IActionResult> UpdateLocationApi(int id, [FromBody] UpdateShipperLocationRequest request)
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
                return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ." });
            if (request == null)
                return BadRequest(new { success = false, message = "Thiếu dữ liệu vị trí." });
            if (request.Latitude < -90 || request.Latitude > 90 || request.Longitude < -180 || request.Longitude > 180)
                return BadRequest(new { success = false, message = "Tọa độ không hợp lệ." });

            var now = DateTime.UtcNow;
            var clientIp = GetClientIp(HttpContext);

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ShipperId == shipperId);
            if (order == null)
                return Conflict(new { success = false, message = "Không thể cập nhật vị trí vì đơn không thuộc shipper hoặc chưa ở trạng thái giao." });

            var ds = order.DeliveryStatus ?? string.Empty;
            var canUpdate = string.Equals(ds, "assigned", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ds, "delivering", StringComparison.OrdinalIgnoreCase);
            if (!canUpdate)
                return Conflict(new { success = false, message = "Không thể cập nhật vị trí vì đơn không thuộc shipper hoặc chưa ở trạng thái giao." });

            order.ShipperLatitude = request.Latitude;
            order.ShipperLongitude = request.Longitude;
            order.ShipperLocationUpdatedAt = now;
            order.ShipperLocationIp = clientIp;
            order.UpdatedAt = now;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, latitude = order.ShipperLatitude, longitude = order.ShipperLongitude, updatedAtUtc = order.ShipperLocationUpdatedAt, clientIp = order.ShipperLocationIp });
        }

        [HttpGet]
        [Route("api/shipper/orders/{id:int}/location")]
        public async Task<IActionResult> GetLocationApi(int id)
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(shipperId))
                return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ." });

            var order = await _db.Orders
                .Where(o => o.Id == id && o.ShipperId == shipperId)
                .Select(o => new
                {
                    o.Id,
                    o.DeliveryStatus,
                    o.ShipperLatitude,
                    o.ShipperLongitude,
                    o.ShipperLocationUpdatedAt,
                    o.ShipperLocationIp
                })
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng." });

            return Ok(new
            {
                success = true,
                orderId = order.Id,
                deliveryStatus = order.DeliveryStatus,
                latitude = order.ShipperLatitude,
                longitude = order.ShipperLongitude,
                updatedAtUtc = order.ShipperLocationUpdatedAt,
                clientIp = order.ShipperLocationIp
            });
        }

        private static string? GetClientIp(HttpContext http)
        {
            var forwarded = http.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var first = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(first))
                    return TruncateClientIp(first);
            }

            var realIp = http.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
                return TruncateClientIp(realIp.Trim());

            return TruncateClientIp(http.Connection.RemoteIpAddress?.ToString());
        }

        private static string? TruncateClientIp(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return null;
            ip = ip.Trim();
            return ip.Length > 100 ? ip[..100] : ip;
        }

        private static ShipperOrderSummaryViewModel MapToSummary(Models.Order order)
        {
            var firstItem = order.OrderDetails.FirstOrDefault();
            var itemCount = order.OrderDetails.Sum(d => d.Quantity);
            return new ShipperOrderSummaryViewModel
            {
                OrderId = order.Id,
                OrderStatus = order.OrderStatus,
                DeliveryStatus = order.DeliveryStatus ?? string.Empty,
                CustomerName = GetCustomerName(order),
                CustomerPhone = order.User?.PhoneNumber,
                DeliveryAddress = order.DeliveryAddress ?? "Chưa có địa chỉ",
                ItemTitle = firstItem?.Product?.Name ?? "Đơn hàng",
                ItemCount = itemCount,
                OrderDate = order.OrderDate,
                TotalPrice = order.TotalPrice,
                AssignedAt = order.AssignedAt,
                DeliveredAt = order.DeliveredAt
            };
        }

        private static string GetCustomerName(Models.Order order)
        {
            var first = order.User?.FirstName?.Trim();
            var last = order.User?.LastName?.Trim();
            var full = $"{first} {last}".Trim();
            return string.IsNullOrWhiteSpace(full) ? (order.User?.UserName ?? "Khách hàng") : full;
        }

        private static decimal GetShipperIncomeForOrder(Models.Order order)
        {
            if (string.IsNullOrWhiteSpace(order.DeliveryAddress))
            {
                return 0m;
            }

            return 20000m;
        }

        private static DateTime? ParseDateDdMmYyyy(string? input, out bool invalid)
        {
            invalid = false;
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var value = input.Trim();
            var acceptedFormats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd" };
            if (DateTime.TryParseExact(value, acceptedFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed;
            }
            if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var fallback))
            {
                return fallback;
            }

            invalid = true;
            return null;
        }

        public sealed class UpdateDeliveryStatusRequest
        {
            public string Status { get; set; } = string.Empty;
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }

        public sealed class UpdateShipperLocationRequest
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}