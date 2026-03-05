using Microsoft.AspNetCore.Mvc;

namespace LaPizzaria.Controllers
{
    public class OrderTrackingController : Controller
    {
        public IActionResult Index()
        {
            //// Vì DB đang lỗi, tôi tạo dữ liệu ảo trực tiếp tại đây để View hiển thị
            //// Bạn có thể sửa các con số này tùy ý để khớp với bài thuyết trình
            //ViewBag.OrderNumber = "PZ-99283";
            //ViewBag.OrderTime = "12:45";
            //ViewBag.EstDelivery = "13:15";
            //ViewBag.Status = "Processing"; // Trạng thái: Pending, Processing, Shipping, Completed

            return View();
        }
    }
}