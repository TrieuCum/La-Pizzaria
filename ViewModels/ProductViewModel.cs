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
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0.")]
        public decimal Price { get; set; }

        [Url(ErrorMessage = "URL Hình ảnh không hợp lệ.")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Danh mục là bắt buộc.")]
        public string Category { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool IsCustomizable { get; set; } = false;
    }
}
