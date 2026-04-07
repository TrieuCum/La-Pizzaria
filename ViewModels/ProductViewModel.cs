using System.ComponentModel.DataAnnotations;

namespace LaPizzaria.ViewModels
{
	public class ProductViewModel
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Tên món ăn là bắt buộc.")]
		[StringLength(100, ErrorMessage = "Tên món ăn không được vượt quá 100 ký tự.")]
		public string Name { get; set; } = string.Empty;

		[StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự.")]
		public string? Description { get; set; }

		[Range(30, 50, ErrorMessage = "% lời từ 30 đến 50.")]
		public decimal ProfitMarginPercent { get; set; } = 30m;

		[Range(0, double.MaxValue, ErrorMessage = "Giá không hợp lệ.")]
		public decimal Price { get; set; }

		[Url(ErrorMessage = "URL hình ảnh không hợp lệ.")]
		public string? ImageUrl { get; set; }

		[Required(ErrorMessage = "Danh mục là bắt buộc.")]
		public string Category { get; set; } = string.Empty;

		public bool IsActive { get; set; } = true;
		public bool IsCustomizable { get; set; }
		public bool IsSlowSeller { get; set; }
		public List<IngredientSelectionViewModel> Ingredients { get; set; } = new();
	}

	public class IngredientSelectionViewModel
	{
		public int IngredientId { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Unit { get; set; } = string.Empty;
		public decimal StockQuantity { get; set; }
		public decimal QuantityPerUnit { get; set; }
		public decimal UnitPrice { get; set; }
		public bool IsDoughBase { get; set; }
		public int? CategoryId { get; set; }
		public int? TabRootCategoryId { get; set; }
		public string CategoryDisplayPath { get; set; } = "Khác";
	}
}
