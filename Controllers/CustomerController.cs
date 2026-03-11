using Microsoft.AspNetCore.Mvc;
using LaPizzaria.Data;
using LaPizzaria.Models;
using System.Linq;
using System.Collections.Generic; // BẮT BUỘC phải có dòng này để dùng List

namespace LaPizzaria.Controllers
{
    public sealed class CustomerController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CustomerController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // TẠO dữ liệu giả để trang web hiện lên mà không cần kết nối SQL đang lỗi
            var customerList = new List<Customer>
            {
                new Customer
                {
                    Id = 1,
                    FullName = "Nguyễn Đinh Hoàng",
                    Phone = "0978238591",
                    Email = "hoang2011@gmail.com",
                    Points = 120
                },
                new Customer
                {
                    Id = 2,
                    FullName = "Nguyễn Văn Toàn",
                    Phone = "0982512961",
                    Email = "toan0102@gmail.com",
                    Points = 450
                }
            };

            return View(customerList);
        }
    }
}