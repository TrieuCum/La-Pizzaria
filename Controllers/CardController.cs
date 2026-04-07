using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    public class CardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Kiểm tra đơn hàng";
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    ViewBag.CurrentUserId = user.Id;
                    ViewBag.UserName = $"{user.FirstName} {user.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(ViewBag.UserName)) ViewBag.UserName = user.UserName ?? user.Email;
                    ViewBag.UserPhone = user.PhoneNumber ?? "";
                    ViewBag.UserAddress = user.Address ?? "";
                }
            }
            return View();
        }
    }
}
