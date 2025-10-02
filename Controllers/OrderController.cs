using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IOrderService _orderService;
        private readonly IQrService _qrService;

        public OrderController(ApplicationDbContext db, IOrderService orderService, IQrService qrService)
        {
            _db = db;
            _orderService = orderService;
            _qrService = qrService;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _db.Orders.Include(o => o.OrderDetails).ToListAsync();
            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
        {
            var details = req.Items.Select(i => new OrderDetail
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.UnitPrice * i.Quantity
            }).ToList();

            var order = await _orderService.CreateOrderAsync(req.UserId, details, req.TableIds ?? new List<int>());
            return Ok(order.Id);
        }

        [HttpPost]
        public async Task<IActionResult> MergeTables(int orderId, [FromBody] List<int> tableIds)
        {
            var ok = await _orderService.MergeTablesAsync(orderId, tableIds);
            if (!ok) return BadRequest("Cannot merge tables currently in use");
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Split(int orderId, [FromBody] Dictionary<int, int> move)
        {
            var orders = await _orderService.SplitOrderAsync(orderId, move);
            return Ok(orders.Select(o => o.Id));
        }

        [HttpGet]
        public IActionResult GenerateQr(string tableCode)
        {
            var payload = _qrService.GenerateTableQrPayload(tableCode);
            return Ok(payload);
        }
    }

    public class CreateOrderRequest
    {
        public string? UserId { get; set; }
        public List<ItemDto> Items { get; set; } = new();
        public List<int>? TableIds { get; set; }
    }

    public class ItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}


