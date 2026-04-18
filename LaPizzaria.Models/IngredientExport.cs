using System;

namespace LaPizzaria.Models
{
    /// <summary>Phiếu xuất hàng / hao hụt nguyên liệu.</summary>
    public class IngredientExport
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public Ingredient? Ingredient { get; set; }
        public DateTime ExportDate { get; set; }
        public decimal Quantity { get; set; }
        /// <summary>Lý do: "Sử dụng" | "Hao hụt" | "Hết hạn" | "Khác".</summary>
        public string Reason { get; set; } = "Sử dụng";
        public string? Note { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
