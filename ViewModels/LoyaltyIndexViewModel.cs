namespace LaPizzaria.ViewModels
{
    public class LoyaltyIndexViewModel
    {
        public int Points { get; set; }
        public string Tier { get; set; } = "Đồng";
        public string NextTierName { get; set; } = "Bạc";
        public int NextTierTarget { get; set; }
        public int RemainingToNextTier { get; set; }
        public int ProgressPercent { get; set; }
        public List<LoyaltyRewardViewModel> Rewards { get; set; } = new();
        public List<LoyaltyTransactionItemViewModel> Transactions { get; set; } = new();
        public List<LoyaltyVoucherItemViewModel> Vouchers { get; set; } = new();
    }

    public class LoyaltyRewardViewModel
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CostPoints { get; set; }
        public bool CanRedeem { get; set; }
    }

    public class LoyaltyTransactionItemViewModel
    {
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public int PointsChange { get; set; }
        public int BalanceAfter { get; set; }
    }

    public class LoyaltyVoucherItemViewModel
    {
        public int VoucherId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Meta { get; set; } = string.Empty;
        public string Status { get; set; } = "unused"; // unused | used | expired
    }
}
