using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Services
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoyaltyService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<bool> RegisterMemberAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsMember) return false;

            user.IsMember = true;
            user.MemberSince = DateTime.UtcNow;
            // Tặng điểm chào mừng khi đăng ký hội viên
            user.LoyaltyPoints += 100;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public Task<int> CalculatePointsAsync(decimal orderTotal)
        {
            // Tính điểm: 1 điểm cho mỗi 10,000đ (làm tròn xuống)
            var points = (int)(orderTotal / 10000);
            return Task.FromResult(points);
        }

        public async Task AwardPointsAsync(string userId, int points, string reason)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsMember) return;

            user.LoyaltyPoints += points;
            await _userManager.UpdateAsync(user);
        }

        public async Task<bool> RedeemRewardAsync(string userId, int rewardId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var reward = await _db.Rewards.FindAsync(rewardId);
            
            if (user == null || reward == null || !user.IsMember) return false;
            if (user.LoyaltyPoints < reward.PointsRequired) return false;
            if (!reward.IsActive) return false;
            if (reward.StockQuantity <= 0 && reward.RewardType != "Discount") return false;

            // Trừ điểm
            user.LoyaltyPoints -= reward.PointsRequired;
            
            // Tạo redemption record
            var redemption = new RewardRedemption
            {
                UserId = userId,
                RewardId = rewardId,
                PointsUsed = reward.PointsRequired,
                Status = "Redeemed",
                RedeemedAt = DateTime.UtcNow,
                ExpiresAt = reward.RewardType == "Discount" ? DateTime.UtcNow.AddMonths(1) : null
            };
            _db.RewardRedemptions.Add(redemption);

            // Giảm stock nếu là vật phẩm
            if (reward.RewardType != "Discount" && reward.StockQuantity > 0)
            {
                reward.StockQuantity--;
            }

            await _userManager.UpdateAsync(user);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
