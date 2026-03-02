using Microsoft.AspNetCore.Mvc;
namespace LaPizzaria.Controllers
{
    public class CustomerController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
}
