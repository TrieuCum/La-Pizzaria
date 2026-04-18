using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
    [Authorize]
    public class LoyaltyController : Controller
    {
        private static readonly List<LoyaltyRewardViewModel> RewardCatalog =
        [
            new() { Code = "DISC10_500", Title = "Giảm 10% tổng hóa đơn", Description = "Áp dụng cho đơn từ 200.000đ, tối đa 50.000đ.", CostPoints = 50 },
            new() { Code = "COCA15_200", Title = "Tặng 1 Coca-Cola 1.5L", Description = "Nhận 1 chai Coca cho bất kỳ đơn hàng.", CostPoints = 20 },
            new() { Code = "FREESHIP5_300", Title = "Freeship dưới 5km", Description = "Miễn phí giao hàng cho khoảng cách < 5km.", CostPoints = 30 },
            new() { Code = "PIZZA_M_2000", Title = "Tặng Pizza size M", Description = "Miễn phí 1 pizza size M dòng Classic.", CostPoints = 150 }
        ];

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoyaltyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            await SyncPointsFromCompletedOrdersAsync(user.Id);

            var customer = await EnsureCustomerAsync(user.Id);
            var points = customer.LoyaltyPoints;
            var (tier, nextTier, nextTarget) = GetTierInfo(points);

            var transactions = await _db.LoyaltyTransactions
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Take(10)
                .ToListAsync();

            var targetedVouchers = await _db.Vouchers
                .Where(v => v.TargetUserId == user.Id)
                .OrderByDescending(v => v.CreatedAtUtc)
                .Take(20)
                .ToListAsync();

            var targetedVoucherIds = targetedVouchers.Select(v => v.Id).ToList();
            var usedVoucherIds = await _db.OrderVouchers
                .Where(ov => targetedVoucherIds.Contains(ov.VoucherId) && ov.Order != null && ov.Order.UserId == user.Id)
                .Select(ov => ov.VoucherId)
                .Distinct()
                .ToListAsync();

            var model = new LoyaltyIndexViewModel
            {
                Points = points,
                Tier = tier,
                NextTierName = nextTier,
                NextTierTarget = nextTarget,
                RemainingToNextTier = Math.Max(0, nextTarget - points),
                ProgressPercent = Math.Clamp(points * 100 / Math.Max(1, nextTarget), 0, 100),
                Rewards = RewardCatalog
                    .Select(r => new LoyaltyRewardViewModel
                    {
                        Code = r.Code,
                        Title = r.Title,
                        Description = r.Description,
                        CostPoints = r.CostPoints,
                        CanRedeem = points >= r.CostPoints
                    }).ToList(),
                Transactions = transactions.Select(t => new LoyaltyTransactionItemViewModel
                {
                    Description = t.Description,
                    CreatedAtUtc = t.CreatedAtUtc,
                    PointsChange = t.PointsChange,
                    BalanceAfter = t.BalanceAfter
                }).ToList(),
                Vouchers = targetedVouchers.Select(v =>
                {
                    var isUsed = usedVoucherIds.Contains(v.Id);
                    var isExpired = v.ExpiresAtUtc.HasValue && v.ExpiresAtUtc.Value < DateTime.UtcNow;
                    var status = isUsed ? "used" : isExpired ? "expired" : "unused";
                    var meta = isUsed
                        ? "Đã dùng trong đơn hàng"
                        : isExpired
                            ? $"Hết hạn: {v.ExpiresAtUtc:dd/MM/yyyy}"
                            : $"HSD: {(v.ExpiresAtUtc.HasValue ? v.ExpiresAtUtc.Value.ToString("dd/MM/yyyy") : "Không giới hạn")}";
                    return new LoyaltyVoucherItemViewModel
                    {
                        VoucherId = v.Id,
                        Title = v.Name,
                        Meta = meta,
                        Status = status
                    };
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Redeem(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var reward = RewardCatalog.FirstOrDefault(r => r.Code == code);
            if (reward == null)
            {
                TempData["error"] = "Ưu đãi không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var strategy = _db.Database.CreateExecutionStrategy();
            var redeemed = false;
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();
                var customer = await EnsureCustomerAsync(user.Id);
                if (customer.LoyaltyPoints < reward.CostPoints)
                {
                    redeemed = false;
                    return;
                }

                customer.LoyaltyPoints -= reward.CostPoints;
                customer.UpdatedAt = DateTime.UtcNow;

                _db.LoyaltyTransactions.Add(new LoyaltyTransaction
                {
                    UserId = user.Id,
                    TransactionType = "Spend",
                    PointsChange = -reward.CostPoints,
                    BalanceAfter = customer.LoyaltyPoints,
                    Description = $"Đổi ưu đãi: {reward.Title}",
                    ReferenceCode = $"REDEEM:{reward.Code}:{Guid.NewGuid():N}",
                    CreatedAtUtc = DateTime.UtcNow
                });

                var voucher = BuildVoucherFromReward(reward, user.Id);
                _db.Vouchers.Add(voucher);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                redeemed = true;
            });

            if (!redeemed)
            {
                TempData["error"] = "Bạn chưa đủ điểm để đổi ưu đãi này.";
                return RedirectToAction(nameof(Index));
            }

            TempData["success"] = "Đổi ưu đãi thành công. Voucher đã thêm vào tài khoản của bạn.";
            return RedirectToAction(nameof(Index));
        }

        private async Task SyncPointsFromCompletedOrdersAsync(string userId)
        {
            var completedOrders = await _db.Orders
                .Where(o => o.UserId == userId && o.OrderStatus == "Completed")
                .OrderBy(o => o.UpdatedAt)
                .ToListAsync();

            var existingRefs = await _db.LoyaltyTransactions
                .Where(t => t.UserId == userId && t.ReferenceCode != null && t.ReferenceCode.StartsWith("ORDER:"))
                .Select(t => t.ReferenceCode!)
                .ToListAsync();
            var existingRefSet = existingRefs.ToHashSet();

            var customer = await EnsureCustomerAsync(userId);
            var changed = false;
            foreach (var order in completedOrders)
            {
                var reference = $"ORDER:{order.Id}";
                if (existingRefSet.Contains(reference)) continue;
                var points = CalculateOrderPoints(order.TotalPrice);
                if (points <= 0) continue;
                customer.LoyaltyPoints += points;
                customer.UpdatedAt = DateTime.UtcNow;
                _db.LoyaltyTransactions.Add(new LoyaltyTransaction
                {
                    UserId = userId,
                    TransactionType = "Earn",
                    PointsChange = points,
                    BalanceAfter = customer.LoyaltyPoints,
                    Description = $"Tích điểm từ đơn hàng #{order.Id}",
                    ReferenceCode = reference,
                    CreatedAtUtc = order.UpdatedAt
                });
                changed = true;
            }

            if (changed)
            {
                await _db.SaveChangesAsync();
            }
        }

        private async Task<Customer> EnsureCustomerAsync(string userId)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer != null) return customer;
            customer = new Customer
            {
                UserId = userId,
                LoyaltyPoints = 0,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
            return customer;
        }

        private static int CalculateOrderPoints(decimal totalPrice)
        {
            // 1 điểm / 10.000đ
            return (int)Math.Floor(totalPrice / 10000m);
        }

        private static (string Tier, string NextTier, int NextTarget) GetTierInfo(int points)
        {
            if (points >= 300) return ("Kim Cương", "Kim Cương", 300);
            if (points >= 150) return ("Vàng", "Kim Cương", 300);
            if (points >= 50) return ("Bạc", "Vàng", 150);
            return ("Đồng", "Bạc", 50);
        }

        private static Voucher BuildVoucherFromReward(LoyaltyRewardViewModel reward, string userId)
        {
            var now = DateTime.UtcNow;
            return reward.Code switch
            {
                "DISC10_500" => new Voucher
                {
                    Code = $"LP10-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                    Name = "Voucher 10% tối đa 50k",
                    VoucherType = "Percentage",
                    DiscountPercent = 10,
                    DiscountAmount = 50000,
                    MinOrderValue = 200000,
                    TargetUserId = userId,
                    IsActive = true,
                    ExpiresAtUtc = now.AddDays(30),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                },
                "COCA15_200" => new Voucher
                {
                    Code = $"COCA-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                    Name = "Tặng Coca-Cola 1.5L",
                    VoucherType = "FixedAmount",
                    DiscountPercent = 0,
                    DiscountAmount = 25000,
                    MinOrderValue = 0,
                    TargetUserId = userId,
                    IsActive = true,
                    ExpiresAtUtc = now.AddDays(15),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                },
                "FREESHIP5_300" => new Voucher
                {
                    Code = $"SHIP-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                    Name = "Freeship dưới 5km",
                    VoucherType = "FreeShipping",
                    DiscountPercent = 0,
                    DiscountAmount = 0,
                    MinOrderValue = 0,
                    TargetUserId = userId,
                    IsActive = true,
                    ExpiresAtUtc = now.AddDays(20),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                },
                _ => new Voucher
                {
                    Code = $"PZM-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
                    Name = "Tặng Pizza size M",
                    VoucherType = "FixedAmount",
                    DiscountPercent = 0,
                    DiscountAmount = 120000,
                    MinOrderValue = 0,
                    TargetUserId = userId,
                    IsActive = true,
                    ExpiresAtUtc = now.AddDays(20),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            };
        }
    }
}

