using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Shipper")]
    public class ShipperController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ShipperNav"] = "Index";
            return View();
        }

        public IActionResult History()
        {
            ViewData["ShipperNav"] = "History";
            return View();
        }

        public IActionResult Income()
        {
            ViewData["ShipperNav"] = "Income";
            return View();
        }
    }
}