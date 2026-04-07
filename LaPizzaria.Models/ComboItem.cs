namespace LaPizzaria.Models
{
	public class ComboItem
	{
		public int Id { get; set; }
		public int ComboId { get; set; }
		public Combo? Combo { get; set; }

		public int ProductId { get; set; }
		public Product? Product { get; set; }

		public int MinQuantity { get; set; } = 1; // quantity of product to qualify
		public decimal? ItemDiscountAmount { get; set; }
		public decimal? ItemDiscountPercent { get; set; }
	}
}


