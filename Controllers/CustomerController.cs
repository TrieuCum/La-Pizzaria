using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    public sealed class CustomerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CustomerController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _db.Customers
                .Include(c => c.User)
                .OrderBy(c => c.Id)
                .ToListAsync();

            var userIds = customers.Select(c => c.UserId).Distinct().ToList();
            var orderCountByUser = await _db.Orders
                .Where(o => userIds.Contains(o.UserId ?? ""))
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var c in customers)
            {
                var u = c.User;
                c.FullName = !string.IsNullOrWhiteSpace(u?.FirstName) || !string.IsNullOrWhiteSpace(u?.LastName)
                    ? $"{u?.FirstName ?? ""} {u?.LastName ?? ""}".Trim()
                    : u?.UserName ?? c.CustomerCode ?? $"Khách #{c.Id}";
                c.Phone = u?.PhoneNumber;
                c.Email = u?.Email;
                c.Points = c.LoyaltyPoints;
                c.OrderCount = orderCountByUser.FirstOrDefault(x => x.UserId == c.UserId)?.Count ?? 0;
            }

            return View(customers);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
    }
}