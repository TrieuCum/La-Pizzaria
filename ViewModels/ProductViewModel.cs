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

        [Required(ErrorMessage = "Giá là bắt buộc.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá không hợp lệ.")]
        public decimal Price { get; set; }

        /// <summary>Phần trăm lời trên giá vốn nguyên liệu (ví dụ 30 = +30%).</summary>
        [Range(0, 500, ErrorMessage = "Phần trăm lời từ 0 đến 500.")]
        public decimal ProfitMarginPercent { get; set; } = 30m;

        public bool IsSlowSeller { get; set; }

        [Url(ErrorMessage = "URL Hình ảnh không hợp lệ.")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Danh mục là bắt buộc.")]
        public string Category { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool IsCustomizable { get; set; } = false;

        public List<IngredientSelectionViewModel> Ingredients { get; set; } = new ();
    }

    public class IngredientSelectionViewModel
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public bool IsDoughBase { get; set; }
        public decimal StockQuantity { get; set; }
        public decimal QuantityPerUnit { get; set; } // selected qty

        public int? CategoryId { get; set; }
        /// <summary>Đường dẫn nhóm: "Nấm" hoặc "Bò / Thịt bò xay".</summary>
        public string CategoryDisplayPath { get; set; } = "Khác";
    }
}
