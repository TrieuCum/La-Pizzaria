using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
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

            var adminStaffRoleIds = await _db.Roles
                .Where(r => r.NormalizedName == "ADMIN" || r.NormalizedName == "STAFF")
                .Select(r => r.Id)
                .ToListAsync();
            var adminStaffUserIds = await _db.UserRoles
                .Where(ur => adminStaffRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            var query = _userManager.Users.AsQueryable();
            query = query.Where(u => !adminStaffUserIds.Contains(u.Id));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    (u.FirstName != null && u.FirstName.ToLower().Contains(term)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(term)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            var customerUsers = await query.OrderBy(u => u.UserName).ToListAsync();
            var userIds = customerUsers.Select(u => u.Id).ToList();

            var customersByUser = await _db.Customers
                .Where(c => userIds.Contains(c.UserId))
                .ToDictionaryAsync(c => c.UserId);
            var orderCountByUser = await _db.Orders
                .Where(o => o.UserId != null && userIds.Contains(o.UserId))
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key!, Count = g.Count() })
                .ToListAsync();

            var list = new List<CustomerListItemViewModel>();
            foreach (var u in customerUsers)
            {
                var cust = customersByUser.GetValueOrDefault(u.Id);
                var status = cust?.Status ?? "Active";
                var points = cust?.LoyaltyPoints ?? 0;
                list.Add(new CustomerListItemViewModel
                {
                    UserId = u.Id,
                    FullName = !string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName)
                        ? $"{u.FirstName} {u.LastName}".Trim()
                        : u.UserName ?? u.Email ?? u.Id,
                    Phone = u.PhoneNumber,
                    Email = u.Email,
                    Points = points,
                    Status = status,
                    OrderCount = orderCountByUser.FirstOrDefault(x => x.UserId == u.Id)?.Count ?? 0,
                    CustomerId = cust?.Id
                });
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                if (statusFilter.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    list = list.Where(c => c.Status == null || c.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) || c.Points > 100).ToList();
                else if (statusFilter.Equals("Locked", StringComparison.OrdinalIgnoreCase))
                    list = list.Where(c => c.Status != null && !c.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) && c.Points <= 100).ToList();
            }

            var totalCount = list.Count;
            var paged = list
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToList();

            var model = new CustomerIndexViewModel
            {
                Customers = paged,
                Search = search,
                StatusFilter = statusFilter,
                Page = page,
                PageSize = DefaultPageSize,
                TotalCount = totalCount
            };
            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            var userDisplay = !string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName)
                ? $"{user.FirstName} {user.LastName}".Trim()
                : user.UserName ?? user.Email ?? userId;

            var model = new CustomerFormViewModel
            {
                Id = customer?.Id,
                UserId = userId,
                UserDisplayName = userDisplay,
                CustomerCode = customer?.CustomerCode,
                LoyaltyPoints = customer?.LoyaltyPoints ?? 0,
                Status = customer?.Status ?? "Active"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerFormViewModel model)
        {
            var userId = model.UserId;
            if (string.IsNullOrEmpty(userId)) return NotFound();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null)
                {
                    customer = new Customer
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Customers.Add(customer);
                }
                customer.CustomerCode = model.CustomerCode;
                customer.LoyaltyPoints = model.LoyaltyPoints;
                customer.Status = model.Status ?? "Active";
                customer.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["success"] = "Đã cập nhật khách hàng.";
                return RedirectToAction(nameof(Index));
            }

            model.UserDisplayName = user.UserName ?? user.Email ?? userId;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer != null)
            {
                _db.Customers.Remove(customer);
                await _db.SaveChangesAsync();
            }
            TempData["success"] = "Đã xóa hồ sơ khách hàng (tài khoản vẫn tồn tại).";
            return RedirectToAction(nameof(Index));
        }
    }
}
