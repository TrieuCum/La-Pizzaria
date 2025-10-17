using System.Collections.Generic;

namespace LaPizzaria.Models
{
	public class Combo
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public decimal DiscountAmount { get; set; } // absolute discount
		public decimal? DiscountPercent { get; set; } // optional percent discount
			public string? ImageUrl { get; set; }
		public bool IsActive { get; set; } = true;

		public ICollection<ComboItem> Items { get; set; } = new List<ComboItem>();
	}
}


