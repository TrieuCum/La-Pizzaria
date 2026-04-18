using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LaPizzaria.Models;
using LaPizzaria.Data;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public EmployeeController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var employeesWithRoles = new List<(ApplicationUser User, string Role)>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                // Chỉ lấy các tài khoản thuộc group nhân viên: Admin, Staff, Shipper
                var mainRole = roles.FirstOrDefault(r =>
                    r == "Admin" || r == "Staff" || r == "Shipper");

                if (mainRole != null)
                {
                    employeesWithRoles.Add((user, mainRole));
                }
            }

            return View(employeesWithRoles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string fullName,
            string phoneNumber,
            string address,
            DateTime? birthday,
            string gender,
            string email,
            string role)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
            {
                TempData["error"] = "Vui lòng nhập đầy đủ Họ tên và Email.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(role) ||
                (role != "Admin" && role != "Staff" && role != "Shipper"))
            {
                TempData["error"] = "Vai trò không hợp lệ. Chỉ được chọn Admin, Staff hoặc Shipper.";
                return RedirectToAction(nameof(Index));
            }

            // Tách full name thành FirstName + LastName (đơn giản: từ cuối là LastName)
            var nameParts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lastName = nameParts.Last();
            var firstName = string.Join(' ', nameParts.Take(nameParts.Length - 1));

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber,
                Address = address,
                Birthday = birthday,
                Gender = gender,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Mật khẩu mặc định cho nhân viên mới
            const string defaultPassword = "Employee@123";
            var result = await _userManager.CreateAsync(user, defaultPassword);
            if (!result.Succeeded)
            {
                TempData["error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            await _userManager.AddToRoleAsync(user, role);

            TempData["success"] = $"Thêm nhân viên thành công. Mật khẩu tạm thời: {defaultPassword}";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetEditData(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            var mainRole = roles.FirstOrDefault(r => r == "Admin" || r == "Staff" || r == "Shipper") ?? "";
            return Json(new
            {
                id = user.Id,
                fullName = $"{user.FirstName} {user.LastName}".Trim(),
                email = user.Email,
                phoneNumber = user.PhoneNumber ?? "",
                address = user.Address ?? "",
                birthday = user.Birthday?.ToString("yyyy-MM-dd"),
                gender = user.Gender ?? "",
                role = mainRole,
                isActive = user.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            string fullName,
            string? phoneNumber,
            string? address,
            DateTime? birthday,
            string? gender,
            string role,
            bool isActive)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["error"] = "Không tìm thấy nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(role) || (role != "Admin" && role != "Staff" && role != "Shipper"))
            {
                TempData["error"] = "Vai trò không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var nameParts = (fullName ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            user.LastName = nameParts.Length > 0 ? nameParts.Last() : "";
            user.FirstName = nameParts.Length > 1 ? string.Join(' ', nameParts.Take(nameParts.Length - 1)) : "";
            user.PhoneNumber = phoneNumber;
            user.Address = address;
            user.Birthday = birthday;
            user.Gender = gender;
            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            var currentRoles = await _userManager.GetRolesAsync(user);
            foreach (var r in currentRoles.Where(r => r == "Admin" || r == "Staff" || r == "Shipper"))
                await _userManager.RemoveFromRoleAsync(user, r);
            await _userManager.AddToRoleAsync(user, role);

            TempData["success"] = "Cập nhật thông tin nhân viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["error"] = "Không tìm thấy nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            TempData["success"] = user.IsActive ? "Đã kích hoạt lại tài khoản nhân viên." : "Đã vô hiệu hóa tài khoản nhân viên.";
            return RedirectToAction(nameof(Index));
        }
    }
}
