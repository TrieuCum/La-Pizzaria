using System.Collections.Generic;

namespace LaPizzaria.Models
{
	public class Ingredient
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Unit { get; set; } = "g";
		public decimal UnitPrice { get; set; }
		public decimal StockQuantity { get; set; }
		public decimal ReorderLevel { get; set; } = 0;
		public bool IsActive { get; set; } = true;
		public bool IsDoughBase { get; set; }
		public bool IsSecondary { get; set; }

		public int? CategoryId { get; set; }
		public IngredientCategory? Category { get; set; }

		public ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
	}
}
