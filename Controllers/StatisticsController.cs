using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using LaPizzaria.Data;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public StatisticsController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            var today = DateTime.UtcNow.Date;
            var startOfChart = today.AddDays(-6);
            
            var ordersInPeriod = _db.Orders
                .Where(o => o.OrderDate >= startOfChart && o.OrderStatus == "Completed")
                .ToList();

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => startOfChart.AddDays(i))
                .ToList();

            var weeklyData = ordersInPeriod
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.TotalPrice) })
                .ToDictionary(x => x.Date, x => x.Revenue);

            ViewBag.ChartLabels = last7Days.Select(d => d.ToString("dd/MM")).ToList();
            ViewBag.ChartValues = last7Days.Select(d => weeklyData.ContainsKey(d) ? (double)weeklyData[d] : 0.0).ToList();

            ViewBag.TotalRevenue = _db.Orders.Where(o => o.OrderStatus == "Completed").Sum(o => (decimal?)o.TotalPrice) ?? 0m;
            ViewBag.TotalOrders = _db.Orders.Count();
            ViewBag.ActiveVouchers = _db.Vouchers.Count(v => v.IsActive && (!v.ExpiresAtUtc.HasValue || v.ExpiresAtUtc > DateTime.UtcNow));
            
            ViewBag.RecentOrders = _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new {
                    o.Id,
                    CustomerName = o.User != null ? (o.User.FirstName + " " + o.User.LastName) : "Khách vãng lai",
                    o.TotalPrice,
                    OrderStatus = o.OrderStatus,
                    OrderDate = o.OrderDate.ToString("dd/MM HH:mm"),
                    ItemsSummary = string.Join(", ", o.OrderDetails.Select(od => od.Product != null ? od.Product.Name : "Món ăn"))
                })
                .ToList();

            return View();
        }

        [HttpGet]
        public IActionResult Daily(DateTime? day)
        {
            ViewBag.Day = (day ?? DateTime.UtcNow).Date;
            return View();
        }

        [HttpGet]
        [Route("Statistics/DailyData")]
        public IActionResult DailyData(DateTime? day)
        {
            var date = (day ?? DateTime.UtcNow).Date;
            var revenue = _db.Orders
                .Where(o => o.OrderDate.Date == date && o.OrderStatus == "Completed")
                .Sum(o => (decimal?)o.TotalPrice) ?? 0m;
            return Ok(new { date, revenue });
        }

        [HttpGet]
        public IActionResult Weekly(DateTime? from, DateTime? to)
        {
            var end = (to ?? DateTime.UtcNow).Date;
            var start = (from ?? end.AddDays(-6)).Date;
            if (start > end) { var tmp = start; start = end; end = tmp; }
            ViewBag.From = start;
            ViewBag.To = end;
            return View();
        }

        [HttpGet]
        [Route("Statistics/WeeklyData")]
        public IActionResult WeeklyData(DateTime? from, DateTime? to)
        {
            var end = (to ?? DateTime.UtcNow).Date;
            var start = (from ?? end.AddDays(-6)).Date;
            if (start > end) { var tmp = start; start = end; end = tmp; }

            var daily = _db.Orders
                .Where(o => o.OrderDate.Date >= start && o.OrderDate.Date <= end && o.OrderStatus == "Completed")
                .AsEnumerable()
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { date = g.Key, revenue = g.Sum(x => x.TotalPrice) })
                .OrderBy(x => x.date)
                .ToList();
            var total = daily.Sum(x => x.revenue);
            return Ok(new { from = start, to = end, total, daily });
        }

        [HttpGet]
        [Route("Statistics/GetChartData")]
        public IActionResult GetChartData(string type)
        {
            var today = DateTime.UtcNow.Date;
            var labels = new List<string>();
            var values = new List<double>();

            if (type == "Daily")
            {
                // Today by hour
                var startOfToday = today;
                var endOfToday = today.AddDays(1);
                var hourlyData = _db.Orders
                    .Where(o => o.OrderDate >= startOfToday && o.OrderDate < endOfToday && o.OrderStatus == "Completed")
                    .AsEnumerable()
                    .GroupBy(o => o.OrderDate.Hour)
                    .ToDictionary(g => g.Key, g => (double)g.Sum(x => x.TotalPrice));

                for (int i = 0; i < 24; i++)
                {
                    labels.Add($"{i}:00");
                    values.Add(hourlyData.ContainsKey(i) ? hourlyData[i] : 0.0);
                }
            }
            else if (type == "Monthly")
            {
                // Last 30 days
                var startOfPeriod = today.AddDays(-29);
                var monthlyData = _db.Orders
                    .Where(o => o.OrderDate >= startOfPeriod && o.OrderStatus == "Completed")
                    .AsEnumerable()
                    .GroupBy(o => o.OrderDate.Date)
                    .ToDictionary(g => g.Key, g => (double)g.Sum(x => x.TotalPrice));

                for (int i = 0; i < 30; i++)
                {
                    var date = startOfPeriod.AddDays(i);
                    labels.Add(date.ToString("dd/MM"));
                    values.Add(monthlyData.ContainsKey(date) ? monthlyData[date] : 0.0);
                }
            }
            else // Weekly is default
            {
                var startOfPeriod = today.AddDays(-6);
                var weeklyData = _db.Orders
                    .Where(o => o.OrderDate >= startOfPeriod && o.OrderStatus == "Completed")
                    .AsEnumerable()
                    .GroupBy(o => o.OrderDate.Date)
                    .ToDictionary(g => g.Key, g => (double)g.Sum(x => x.TotalPrice));

                for (int i = 0; i < 7; i++)
                {
                    var date = startOfPeriod.AddDays(i);
                    labels.Add(date.ToString("dd/MM"));
                    values.Add(weeklyData.ContainsKey(date) ? weeklyData[date] : 0.0);
                }
            }

            return Json(new { labels, values });
        }
    }
}


