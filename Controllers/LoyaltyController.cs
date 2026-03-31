using System.Threading.Tasks;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    [Authorize]
    public class LoyaltyController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoyaltyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            var points = customer?.LoyaltyPoints ?? 0;

            ViewBag.Points = points;
            ViewBag.Tier = points >= 1500 ? "Vàng" : points >= 500 ? "Bạc" : "Đồng";
            ViewBag.NextTierName = points >= 1500 ? "Kim Cương" : "Vàng";
            ViewBag.NextTierTarget = points >= 1500 ? 3000 : 1500;

            return View();
        }
    }
}

