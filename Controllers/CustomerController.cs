using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin")]
    public sealed class CustomerController : Controller
    {
        private const int DefaultPageSize = 10;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, string? statusFilter, int page = 1)
        {
            if (page < 1) page = 1;

            var query = _db.Customers.Include(c => c.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    (c.User != null && (
                        (c.User.FirstName != null && c.User.FirstName.ToLower().Contains(term)) ||
                        (c.User.LastName != null && c.User.LastName.ToLower().Contains(term)) ||
                        (c.User.UserName != null && c.User.UserName.ToLower().Contains(term)) ||
                        (c.User.Email != null && c.User.Email.ToLower().Contains(term)) ||
                        (c.User.PhoneNumber != null && c.User.PhoneNumber.Contains(term))
                    )) ||
                    (c.CustomerCode != null && c.CustomerCode.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                if (statusFilter.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => c.Status == null || c.Status == "Active" || c.LoyaltyPoints > 100);
                else if (statusFilter.Equals("Locked", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => c.Status != null && !c.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) && c.LoyaltyPoints <= 100);
            }

            var totalCount = await query.CountAsync();
            var customers = await query
                .OrderBy(c => c.Id)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
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

            var model = new CustomerIndexViewModel
            {
                Customers = customers,
                Search = search,
                StatusFilter = statusFilter,
                Page = page,
                PageSize = DefaultPageSize,
                TotalCount = totalCount
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var existingCustomerUserIds = await _db.Customers.Select(c => c.UserId).ToListAsync();
            var users = await _userManager.Users
                .Where(u => !existingCustomerUserIds.Contains(u.Id))
                .OrderBy(u => u.UserName)
                .Select(u => new { u.Id, Display = (u.FirstName + " " + u.LastName).Trim() != "" ? (u.FirstName + " " + u.LastName).Trim() : u.UserName ?? u.Email ?? u.Id })
                .ToListAsync();
            ViewBag.AvailableUsers = users;
            return View(new CustomerFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerFormViewModel model)
        {
            var existingCustomerUserIds = await _db.Customers.Select(c => c.UserId).ToListAsync();
            if (existingCustomerUserIds.Contains(model.UserId))
            {
                ModelState.AddModelError("UserId", "Tài khoản này đã là khách hàng.");
            }

            if (ModelState.IsValid)
            {
                var customer = new Customer
                {
                    UserId = model.UserId,
                    CustomerCode = model.CustomerCode,
                    LoyaltyPoints = model.LoyaltyPoints,
                    Status = model.Status ?? "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();
                TempData["success"] = "Đã thêm khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            var users = await _userManager.Users
                .Where(u => !existingCustomerUserIds.Contains(u.Id))
                .OrderBy(u => u.UserName)
                .Select(u => new { u.Id, Display = (u.FirstName + " " + u.LastName).Trim() != "" ? (u.FirstName + " " + u.LastName).Trim() : u.UserName ?? u.Email ?? u.Id })
                .ToListAsync();
            ViewBag.AvailableUsers = users;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _db.Customers.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id);
            if (customer == null) return NotFound();

            var userDisplay = customer.User != null
                ? ($"{customer.User.FirstName} {customer.User.LastName}".Trim() != "" ? $"{customer.User.FirstName} {customer.User.LastName}".Trim() : customer.User.UserName ?? customer.User.Email ?? customer.UserId)
                : customer.UserId;

            var model = new CustomerFormViewModel
            {
                Id = customer.Id,
                UserId = customer.UserId,
                UserDisplayName = userDisplay,
                CustomerCode = customer.CustomerCode,
                LoyaltyPoints = customer.LoyaltyPoints,
                Status = customer.Status
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerFormViewModel model)
        {
            if (id != model.Id) return NotFound();
            var customer = await _db.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            if (ModelState.IsValid)
            {
                customer.CustomerCode = model.CustomerCode;
                customer.LoyaltyPoints = model.LoyaltyPoints;
                customer.Status = model.Status;
                customer.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["success"] = "Đã cập nhật khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            model.UserDisplayName = customer.UserId;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            _db.Customers.Remove(customer);
            await _db.SaveChangesAsync();
            TempData["success"] = "Đã xóa khách hàng.";
            return RedirectToAction(nameof(Index));
        }
    }
}
