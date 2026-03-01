using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    public class CardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Kiểm tra đơn hàng";
            return View();
        }
    }
}
