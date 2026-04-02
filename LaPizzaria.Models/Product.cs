using System;
using System.Collections.Generic;

namespace LaPizzaria.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        /// <summary>Phần trăm lời áp vào giá vốn nguyên liệu (vd: 30 = +30%). Dùng khi món có định lượng nguyên liệu.</summary>
        public decimal ProfitMarginPercent { get; set; } = 30m;
        public string? ImageUrl { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsCustomizable { get; set; } = false;
        /// <summary>Món bán chậy — dùng cho voucher upsale (giỏ phải có ít nhất một món bán chậy).</summary>
        public bool IsSlowSeller { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<ProductTopping> ProductToppings { get; set; } = new List<ProductTopping>();
        public ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
    }
}
