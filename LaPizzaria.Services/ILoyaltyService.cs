using LaPizzaria.Models;

namespace LaPizzaria.Services
{
    public interface ILoyaltyService
    {
        Task<bool> RegisterMemberAsync(string userId);
        Task<int> CalculatePointsAsync(decimal orderTotal);
        Task AwardPointsAsync(string userId, int points, string reason);
        Task<bool> RedeemRewardAsync(string userId, int rewardId);
    }
}
