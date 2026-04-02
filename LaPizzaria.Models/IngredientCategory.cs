using System.Collections.Generic;

namespace LaPizzaria.Models
{
	/// <summary>Nhóm nguyên liệu (có thể lồng nhóm con).</summary>
	public class IngredientCategory
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public int? ParentId { get; set; }
		public IngredientCategory? Parent { get; set; }
		public ICollection<IngredientCategory> Children { get; set; } = new List<IngredientCategory>();
		public int SortOrder { get; set; }

		public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
	}
}
