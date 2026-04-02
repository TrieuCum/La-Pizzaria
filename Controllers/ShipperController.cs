using System.Globalization;
using System.Security.Claims;
using LaPizzaria.Data;
using LaPizzaria.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
                .Where(o =>
                    o.ShipperId == null &&
                    (o.OrderStatus == "Confirmed" ||
                     o.OrderStatus == "Preparing" ||
                     o.OrderStatus == "Ready" ||
                     o.OrderStatus == "Đang chế biến" ||
                     o.OrderStatus == "Sẵn sàng"))
                .OrderBy(o => o.OrderDate)
                .Take(8)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var todaysDelivered = await _db.Orders
                .Where(o => o.OrderStatus == "Completed"
                    && !string.IsNullOrWhiteSpace(o.DeliveryAddress)
                    && o.UpdatedAt.Date == today)
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

            var completedForIncome = await query
                .Where(o => o.OrderStatus == "Completed")
                .ToListAsync();
            var income = completedForIncome.Sum(GetShipperIncomeForOrder);

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
    UpdatedAt = {DateTime.UtcNow}
WHERE Id = {id}
  AND ShipperId IS NULL
  AND (OrderStatus = {"Confirmed"} OR OrderStatus = {"Preparing"} OR OrderStatus = {"Ready"} OR OrderStatus = {"Đang chế biến"} OR OrderStatus = {"Sẵn sàng"})
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
        public async Task<IActionResult> AcceptApi(int id)
        {
            var shipperId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(shipperId))
                return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ." });

            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE Orders
SET OrderStatus = {"Delivering"},
    ShipperId = {shipperId},
    UpdatedAt = {DateTime.UtcNow}
WHERE Id = {id}
  AND ShipperId IS NULL
  AND (OrderStatus = {"Confirmed"} OR OrderStatus = {"Preparing"} OR OrderStatus = {"Ready"} OR OrderStatus = {"Đang chế biến"} OR OrderStatus = {"Sẵn sàng"})
");

            if (affectedRows == 0)
                return Conflict(new { success = false, message = "Đơn đã được shipper khác nhận" });

            return Ok(new { success = true, message = "Nhận đơn thành công", orderStatus = "Delivering" });
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

        private static decimal GetShipperIncomeForOrder(Models.Order order)
        {
            if (string.IsNullOrWhiteSpace(order.DeliveryAddress))
            {
                return 0m;
            }

            return 20000m;
        }
    }
}