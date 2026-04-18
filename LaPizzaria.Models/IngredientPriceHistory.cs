using System;

namespace LaPizzaria.Models
{
    public class IngredientPriceHistory
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public Ingredient? Ingredient { get; set; }
        /// <summary>Ngày ghi nhận giá (chỉ lấy phần Date).</summary>
        public DateTime RecordedDate { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
