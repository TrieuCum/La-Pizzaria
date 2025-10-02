namespace LaPizzaria.Models
{
	public class ProductIngredient
	{
		public int ProductId { get; set; }
		public Product? Product { get; set; }

		public int IngredientId { get; set; }
		public Ingredient? Ingredient { get; set; }

		public decimal QuantityPerUnit { get; set; } // how much ingredient per product unit
	}
}


