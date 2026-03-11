using Microsoft.AspNetCore.Mvc;

namespace LaPizzaria.Controllers
{
    public class ShipperController : Controller
    {
       
        public IActionResult Index()
        {
         
            return View();
        }
    }
}