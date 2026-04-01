using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    public class VoucherCustomerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public VoucherCustomerController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        /// <param name="category">All | Pizza | Drink | Side Dish | Combo — bộ lọc loại món</param>
        /// <param name="tab">vouchers | monan | vanchuyen | thanhvien — tab đang chọn</param>
        public async Task<IActionResult> Index(string? category, string? tab)
        {
            var currentCategory = string.IsNullOrWhiteSpace(category) ? "All" : category;
            var currentTab = !string.IsNullOrWhiteSpace(tab) ? tab : (!string.IsNullOrWhiteSpace(category) ? "monan" : "vouchers");
            var userId = _userManager.GetUserId(User);
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            var vouchersQuery = _db.Vouchers
                .Include(v => v.TargetProduct)
                .Where(v => v.IsActive && (v.ExpiresAt == null || v.ExpiresAt > DateTime.Now))
                .AsQueryable();

            vouchersQuery = isAuthenticated && !string.IsNullOrEmpty(userId)
                ? vouchersQuery.Where(v => v.TargetUserId == null || v.TargetUserId == userId)
                : vouchersQuery.Where(v => v.TargetUserId == null);

            var vouchers = await vouchersQuery
                .OrderByDescending(v => v.DiscountPercent)
                .ToListAsync();

            var productsQuery = _db.Products.Where(p => p.IsActive);
            if (currentCategory != "All" && currentCategory != "Combo")
                productsQuery = productsQuery.Where(p => p.Category == currentCategory);

            var products = await productsQuery.ToListAsync();

            var combosQuery = _db.Combos
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .Where(c => c.IsActive);
            if (currentCategory != "All" && currentCategory != "Combo")
                combosQuery = combosQuery.Where(c => false); // khi chọn Pizza/Drink/Side Dish thì không hiện combo
            var combos = await combosQuery.ToListAsync();

            var categoryFilters = new List<CategoryFilterItem>
            {
                new() { Value = "All", Label = "Tất cả" },
                new() { Value = "Pizza", Label = "Pizza" },
                new() { Value = "Combo", Label = "Combo" },
                new() { Value = "Drink", Label = "Đồ uống" },
                new() { Value = "Side Dish", Label = "Món ăn kèm" }
            };

            var savedVoucherIds = new List<int>();
            if (isAuthenticated)
            {
                if (!string.IsNullOrEmpty(userId))
                    savedVoucherIds = await _db.UserSavedVouchers
                        .Where(usv => usv.UserId == userId)
                        .Select(usv => usv.VoucherId)
                        .ToListAsync();
            }

            var vm = new VoucherCustomerIndexViewModel
            {
                Vouchers = vouchers,
                Products = products,
                Combos = combos,
                CurrentCategory = currentCategory,
                CurrentTab = currentTab,
                CategoryFilters = categoryFilters,
                SavedVoucherIds = savedVoucherIds,
                IsAuthenticated = isAuthenticated
            };

            return View(vm);
        }

        /// <summary>Lưu voucher vào tài khoản (bấm "Lưu mã"). Yêu cầu đăng nhập.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> SaveVoucher(int voucherId, CancellationToken cancellationToken = default)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Vui lòng đăng nhập để lưu mã." });

            var voucher = await _db.Vouchers.FindAsync(new object[] { voucherId }, cancellationToken);
            if (voucher == null)
                return Json(new { success = false, message = "Mã không tồn tại." });
            if (!voucher.IsActive || (voucher.ExpiresAt.HasValue && voucher.ExpiresAt.Value < DateTime.Now))
                return Json(new { success = false, message = "Mã đã hết hạn hoặc không còn hiệu lực." });
            if (!string.IsNullOrEmpty(voucher.TargetUserId) && voucher.TargetUserId != userId)
                return Json(new { success = false, message = "Mã ưu đãi này không thuộc tài khoản của bạn." });

            var existing = await _db.UserSavedVouchers
                .AnyAsync(usv => usv.UserId == userId && usv.VoucherId == voucherId, cancellationToken);
            if (existing)
                return Json(new { success = true, message = "Bạn đã lưu mã này rồi.", alreadySaved = true });

            _db.UserSavedVouchers.Add(new UserSavedVoucher { UserId = userId, VoucherId = voucherId });
            await _db.SaveChangesAsync(cancellationToken);

            return Json(new { success = true, message = "Đã lưu mã vào tài khoản.", code = voucher.Code });
        }
    }
}
