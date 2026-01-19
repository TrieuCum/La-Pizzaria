using System;

namespace LaPizzaria.Models
{
    public class Reward
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int PointsRequired { get; set; } // Số điểm cần để đổi
        public string RewardType { get; set; } = string.Empty; // "FreeDrink", "FreeSide", "FreeToy", "Discount"
        public decimal? DiscountAmount { get; set; } // Nếu là discount
        public decimal? DiscountPercent { get; set; } // Nếu là discount %
        public bool IsActive { get; set; } = true;
        public int StockQuantity { get; set; } = 0; // Số lượng còn lại (nếu là vật phẩm)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
