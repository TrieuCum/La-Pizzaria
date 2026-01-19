using System;

namespace LaPizzaria.Models
{
    public class RewardRedemption
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int RewardId { get; set; }
        public int PointsUsed { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Redeemed, Expired
        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // Thời hạn sử dụng phần thưởng

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Reward Reward { get; set; } = null!;
    }
}
