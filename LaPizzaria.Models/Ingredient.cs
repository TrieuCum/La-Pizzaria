using System.Collections.Generic;

namespace LaPizzaria.Models
{
	public class Ingredient
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Unit { get; set; } = "g"; // default gram
		public decimal StockQuantity { get; set; } // current stock in unit
		public decimal ReorderLevel { get; set; } = 0;
		/// <summary>Giá vốn (₫) cho 1 đơn vị đo <see cref="Unit"/> (vd: 1g, 1ml, 1kg tùy cách nhập).</summary>
		public decimal UnitPrice { get; set; }
		public bool IsActive { get; set; } = true;

		public int? CategoryId { get; set; }
		public IngredientCategory? Category { get; set; }
		/// <summary>Nguyên liệu phụ (hiển thị trong nhóm).</summary>
		public bool IsSecondary { get; set; }

		// Navigation
		public ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
	}
}


