using Microsoft.AspNetCore.Mvc;

namespace LaPizzaria.Controllers
{
    public class OrderTrackingController : Controller
    {
        public IActionResult Index()
        {
            
            return View();
        }
    }
}