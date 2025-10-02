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
            var date = (day ?? DateTime.UtcNow).Date;
            var revenue = _db.Orders
                .Where(o => o.OrderDate.Date == date)
                .Sum(o => (decimal?)o.TotalPrice) ?? 0m;
            return Ok(new { date, revenue });
        }

        [HttpGet]
        public IActionResult Weekly(DateTime? from)
        {
            var start = (from ?? DateTime.UtcNow).Date.AddDays(-6);
            var end = start.AddDays(6);
            var daily = _db.Orders
                .Where(o => o.OrderDate.Date >= start && o.OrderDate.Date <= end)
                .AsEnumerable()
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { date = g.Key, revenue = g.Sum(x => x.TotalPrice) })
                .OrderBy(x => x.date)
                .ToList();
            return Ok(daily);
        }
    }
}


