using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
using LaPizzaria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace LaPizzaria.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.EmailOrUserName, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                TempData["success"] = "Đăng nhập thành công. Bạn có thể bắt đầu đặt món.";
                return RedirectToAction("Index", "Home");
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản tạm thời bị khóa. Vui lòng thử lại sau.");
                return View(model);
            }
            ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không đúng.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName ?? string.Empty,
                LastName = model.LastName ?? string.Empty,
                PhoneNumber = model.PhoneNumber
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["success"] = "Đăng ký thành công. Chào mừng đến LaPizzaria!";
                return RedirectToAction("Index", "Home");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RegisterMember()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (user.IsMember)
            {
                TempData["error"] = "Bạn đã là hội viên rồi!";
                return RedirectToAction("Manage");
            }

            var loyaltyService = HttpContext.RequestServices.GetRequiredService<ILoyaltyService>();
            var success = await loyaltyService.RegisterMemberAsync(user.Id);
            
            if (success)
            {
                TempData["success"] = "Đăng ký hội viên thành công! Bạn đã nhận được 100 điểm chào mừng.";
            }
            else
            {
                TempData["error"] = "Có lỗi xảy ra khi đăng ký hội viên.";
            }

            return RedirectToAction("Manage");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            var vm = new ManageAccountViewModel
            {
                UserName = user.UserName ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.AvatarUrl ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                DefaultAddress = user.DefaultAddress,
                LoyaltyPoints = user.LoyaltyPoints,
                IsMember = user.IsMember
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Manage(ManageAccountViewModel model, IFormFile? avatarFile)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            // Handle avatar upload
            if (avatarFile != null && avatarFile.Length > 0)
            {
                // Validate file size (max 2MB)
                if (avatarFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError(string.Empty, "File ảnh không được vượt quá 2MB.");
                    return View(model);
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError(string.Empty, "Chỉ chấp nhận file ảnh (JPG, PNG, GIF).");
                    return View(model);
                }

                // Save file
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{user.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                user.AvatarUrl = $"/uploads/avatars/{uniqueFileName}";
            }
            else if (!string.IsNullOrWhiteSpace(model.AvatarUrl))
            {
                // Use URL if provided
                user.AvatarUrl = model.AvatarUrl;
            }

            // Update profile fields
            user.FirstName = model.FirstName ?? string.Empty;
            user.LastName = model.LastName ?? string.Empty;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;
            user.DefaultAddress = model.DefaultAddress;
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName != user.UserName)
            {
                var setName = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setName.Succeeded)
                {
                    foreach (var e in setName.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View(model);
                }
            }

            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                foreach (var e in update.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            // Change password if provided
            if (!string.IsNullOrWhiteSpace(model.CurrentPassword) && !string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View(model);
                }
            }

            TempData["success"] = "Cập nhật tài khoản thành công.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var db = HttpContext.RequestServices.GetRequiredService<LaPizzaria.Data.ApplicationDbContext>();
            var orders = await db.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    id = o.Id,
                    orderCode = o.OrderCode,
                    orderDate = o.OrderDate,
                    totalPrice = o.TotalPrice,
                    orderStatus = o.OrderStatus
                })
                .ToListAsync();

            return Json(orders);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyReviews()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var db = HttpContext.RequestServices.GetRequiredService<LaPizzaria.Data.ApplicationDbContext>();
            var reviews = await db.Reviews
                .Include(r => r.Order)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    orderCode = r.Order.OrderCode,
                    rating = r.Rating,
                    comment = r.Comment,
                    imageUrl = r.ImageUrl,
                    createdAt = r.CreatedAt,
                    adminResponse = r.AdminReply
                })
                .ToListAsync();

            return Json(reviews);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyFavorites()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var db = HttpContext.RequestServices.GetRequiredService<LaPizzaria.Data.ApplicationDbContext>();
            var favorites = await db.FavoriteProducts
                .Include(fp => fp.Product)
                .Where(fp => fp.UserId == user.Id)
                .Select(fp => new
                {
                    id = fp.Product.Id,
                    name = fp.Product.Name,
                    price = fp.Product.Price,
                    imageUrl = fp.Product.ImageUrl
                })
                .ToListAsync();

            return Json(favorites);
        }
    }
}
