using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken] // Allow JSON requests
        public async Task<IActionResult> Add([FromBody] AddToCartRequest? request)
        {
            if (request == null)
            {
                // Try to get from form data as fallback
                var productIdParam = Request.Form["productId"].FirstOrDefault();
                var quantityParam = Request.Form["quantity"].FirstOrDefault();
                
                if (string.IsNullOrEmpty(productIdParam) || !int.TryParse(productIdParam, out var productId))
                {
                    return BadRequest(new { success = false, message = "Invalid request" });
                }
                
                var quantity = 1;
                if (!string.IsNullOrEmpty(quantityParam) && int.TryParse(quantityParam, out var qty))
                {
                    quantity = qty;
                }
                
                request = new AddToCartRequest { ProductId = productId, Quantity = quantity };
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var product = await _db.Products.FindAsync(request.ProductId);
            if (product == null || !product.IsActive) return NotFound();

            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == user.Id && ci.ProductId == request.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
                existingItem.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserId = user.Id,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true, message = "Đã thêm vào giỏ hàng" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int itemId, int change)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var item = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.UserId == user.Id);

            if (item == null) return NotFound();

            item.Quantity += change;
            if (item.Quantity <= 0)
            {
                _db.CartItems.Remove(item);
            }
            else
            {
                item.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int itemId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var item = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.UserId == user.Id);

            if (item == null) return NotFound();

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var cartItems = await _db.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == user.Id)
                .Select(ci => new
                {
                    id = ci.Id,
                    productId = ci.ProductId,
                    name = ci.Product.Name,
                    imageUrl = ci.Product.ImageUrl,
                    price = ci.Product.Price,
                    quantity = ci.Quantity,
                    subtotal = ci.Product.Price * ci.Quantity
                })
                .ToListAsync();

            var total = cartItems.Sum(ci => ci.subtotal);
            var itemCount = cartItems.Sum(ci => ci.quantity);

            return Ok(new { items = cartItems, total, itemCount });
        }
    }

    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
