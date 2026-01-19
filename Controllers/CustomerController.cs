using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LaPizzaria.Models;
using LaPizzaria.Data;
using LaPizzaria.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CustomerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public CustomerController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index(string search, string membershipStatus, string sortBy)
        {
            var query = _userManager.Users.AsQueryable();

            // Filter by search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => 
                    u.Email.Contains(search) || 
                    u.FirstName.Contains(search) || 
                    u.LastName.Contains(search) ||
                    u.PhoneNumber.Contains(search));
            }

            // Filter by membership status
            if (!string.IsNullOrEmpty(membershipStatus))
            {
                if (membershipStatus == "Members")
                {
                    query = query.Where(u => u.IsMember == true);
                }
                else if (membershipStatus == "NonMembers")
                {
                    query = query.Where(u => !u.IsMember);
                }
            }

            // Sort
            switch (sortBy)
            {
                case "points_desc":
                    query = query.OrderByDescending(u => u.LoyaltyPoints);
                    break;
                case "points_asc":
                    query = query.OrderBy(u => u.LoyaltyPoints);
                    break;
                case "member_since":
                    query = query.OrderByDescending(u => u.MemberSince);
                    break;
                case "name":
                    query = query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
                    break;
                default:
                    query = query.OrderByDescending(u => u.CreatedAt);
                    break;
            }

            var customers = await query
                .Include(u => u.Orders)
                .ToListAsync();

            // Calculate additional stats for each customer
            var customerViewModels = customers.Select(c => new CustomerViewModel
            {
                User = c,
                TotalOrders = c.Orders.Count,
                TotalSpent = c.Orders.Sum(o => o.TotalPrice),
                LastOrderDate = c.Orders.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate
            }).ToList();

            ViewBag.Search = search ?? string.Empty;
            ViewBag.MembershipStatus = membershipStatus ?? "All";
            ViewBag.SortBy = sortBy ?? "newest";

            return View(customerViewModels);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _userManager.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .Include(u => u.Reviews)
                .Include(u => u.FavoriteProducts)
                    .ThenInclude(fp => fp.Product)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            var viewModel = new CustomerDetailViewModel
            {
                User = customer,
                TotalOrders = customer.Orders.Count,
                TotalSpent = customer.Orders.Sum(o => o.TotalPrice),
                AverageOrderValue = customer.Orders.Any() ? customer.Orders.Average(o => o.TotalPrice) : 0,
                TotalReviews = customer.Reviews.Count,
                FavoriteProducts = customer.FavoriteProducts.Select(fp => fp.Product).ToList(),
                RecentOrders = customer.Orders.OrderByDescending(o => o.OrderDate).Take(10).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMembership(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _userManager.FindByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.IsMember = !customer.IsMember;
            if (customer.IsMember && customer.MemberSince == null)
            {
                customer.MemberSince = DateTime.UtcNow;
            }

            var result = await _userManager.UpdateAsync(customer);
            if (result.Succeeded)
            {
                TempData["success"] = customer.IsMember 
                    ? "Đã kích hoạt tư cách hội viên" 
                    : "Đã hủy tư cách hội viên";
            }
            else
            {
                TempData["error"] = "Có lỗi xảy ra khi cập nhật";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustPoints(string id, int points, string reason)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer = await _userManager.FindByIdAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.LoyaltyPoints += points;
            if (customer.LoyaltyPoints < 0)
            {
                customer.LoyaltyPoints = 0;
            }

            var result = await _userManager.UpdateAsync(customer);
            if (result.Succeeded)
            {
                TempData["success"] = $"Đã điều chỉnh điểm: {(points >= 0 ? "+" : "")}{points} điểm. Lý do: {reason}";
            }
            else
            {
                TempData["error"] = "Có lỗi xảy ra khi cập nhật điểm";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
