using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace LaPizzaria.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoyaltyService _loyaltyService;

        public ReviewController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILoyaltyService loyaltyService)
        {
            _db = db;
            _userManager = userManager;
            _loyaltyService = loyaltyService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var reviews = await _db.Reviews
                .Include(r => r.Order)
                .Include(r => r.Product)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null) return NotFound();
            if (order.OrderStatus != OrderStatus.Completed)
            {
                TempData["error"] = "Chỉ có thể đánh giá đơn hàng đã hoàn thành.";
                return RedirectToAction("Index", "Order");
            }

            // Check if already reviewed
            var existingReview = await _db.Reviews.FirstOrDefaultAsync(r => r.OrderId == orderId && r.UserId == user.Id);
            if (existingReview != null)
            {
                return RedirectToAction("Edit", new { id = existingReview.Id });
            }

            ViewBag.Order = order;
            return View(new Review { OrderId = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review model, IFormFile? reviewImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var order = await _db.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == model.OrderId);
                ViewBag.Order = order;
                return View(model);
            }

            // Handle image upload
            if (reviewImage != null && reviewImage.Length > 0)
            {
                if (reviewImage.Length > 5 * 1024 * 1024) // 5MB max
                {
                    ModelState.AddModelError(string.Empty, "Hình ảnh không được vượt quá 5MB.");
                    var order = await _db.Orders
                        .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                        .FirstOrDefaultAsync(o => o.Id == model.OrderId);
                    ViewBag.Order = order;
                    return View(model);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reviews");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = Path.GetExtension(reviewImage.FileName).ToLowerInvariant();
                var uniqueFileName = $"{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await reviewImage.CopyToAsync(stream);
                }

                model.ImageUrl = $"/uploads/reviews/{uniqueFileName}";
            }

            model.UserId = user.Id;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            // Award points for review (50 points)
            model.PointsAwarded = 50;
            if (user.IsMember)
            {
                await _loyaltyService.AwardPointsAsync(user.Id, 50, "Đánh giá đơn hàng");
            }

            _db.Reviews.Add(model);
            await _db.SaveChangesAsync();

            TempData["success"] = "Đánh giá thành công! Bạn đã nhận được 50 điểm thưởng.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var review = await _db.Reviews
                .Include(r => r.Order)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

            if (review == null) return NotFound();

            ViewBag.Order = review.Order;
            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Review model, IFormFile? reviewImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);
            if (review == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var order = await _db.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.Id == review.OrderId);
                ViewBag.Order = order;
                return View(review);
            }

            // Handle image upload
            if (reviewImage != null && reviewImage.Length > 0)
            {
                if (reviewImage.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(string.Empty, "Hình ảnh không được vượt quá 5MB.");
                    var order = await _db.Orders
                        .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                        .FirstOrDefaultAsync(o => o.Id == review.OrderId);
                    ViewBag.Order = order;
                    return View(review);
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reviews");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = Path.GetExtension(reviewImage.FileName).ToLowerInvariant();
                var uniqueFileName = $"{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await reviewImage.CopyToAsync(stream);
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(review.ImageUrl) && review.ImageUrl.StartsWith("/uploads/reviews/"))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", review.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                review.ImageUrl = $"/uploads/reviews/{uniqueFileName}";
            }

            review.Rating = model.Rating;
            review.Comment = model.Comment;
            review.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["success"] = "Cập nhật đánh giá thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string adminReply)
        {
            var review = await _db.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.AdminReply = adminReply;
            review.AdminReplyDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["success"] = "Đã gửi phản hồi.";
            return RedirectToAction("Index", "Review", new { area = "" });
        }
    }
}
