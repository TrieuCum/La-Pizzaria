using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Shipper")]
    public class ShipperController : Controller
    {
        public IActionResult Index()
        {
         
            return View();
        }
    }
}