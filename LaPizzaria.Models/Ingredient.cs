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
		public bool IsActive { get; set; } = true;

		// Navigation
		public ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
	}
}


