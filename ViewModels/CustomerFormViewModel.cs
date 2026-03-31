using System.ComponentModel.DataAnnotations;

namespace LaPizzaria.ViewModels
{
    public class CustomerFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Chọn tài khoản người dùng.")]
        [Display(Name = "Tài khoản")]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Mã khách hàng")]
        public string? CustomerCode { get; set; }

        [Display(Name = "Điểm tích lũy")]
        [Range(0, int.MaxValue, ErrorMessage = "Điểm tích lũy phải >= 0")]
        public int LoyaltyPoints { get; set; }

        [StringLength(100)]
        [Display(Name = "Trạng thái")]
        public string? Status { get; set; }

        /// <summary>Chỉ dùng cho Edit: hiển thị tên user đã chọn (readonly).</summary>
        public string? UserDisplayName { get; set; }
    }
}
