using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;

namespace LaPizzaria.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public StatisticsController(ApplicationDbContext db)
        {
            _db = db;
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
                .Where(o => o.OrderDate.Date == date)
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
                .Where(o => o.OrderDate.Date >= start && o.OrderDate.Date <= end)
                .AsEnumerable()
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { date = g.Key, revenue = g.Sum(x => x.TotalPrice) })
                .OrderBy(x => x.date)
                .ToList();
            var total = daily.Sum(x => x.revenue);
            return Ok(new { from = start, to = end, total, daily });
        }
    }
}


