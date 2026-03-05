using Microsoft.AspNetCore.Mvc;

namespace YourProjectName.Controllers
{
    public class VoucherCustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}