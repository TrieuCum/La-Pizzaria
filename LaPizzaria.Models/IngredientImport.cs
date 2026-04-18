using System;

namespace LaPizzaria.Models
{
    /// <summary>Phiếu nhập hàng nguyên liệu.</summary>
    public class IngredientImport
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public Ingredient? Ingredient { get; set; }
        public DateTime ImportDate { get; set; }
        public decimal Quantity { get; set; }
        /// <summary>Giá nhập mỗi đơn vị tại thời điểm nhập.</summary>
        public decimal UnitPrice { get; set; }
        public string? Supplier { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
