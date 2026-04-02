using System.ComponentModel.DataAnnotations;

namespace LaPizzaria.Models;

/// <summary>
/// Thông tin đơn hàng gửi từ View/API để khởi tạo thanh toán MoMo.
/// </summary>
public class OrderInfoModel
{
    /// <summary>Mã hoặc ID đơn hàng hiển thị cho MoMo.</summary>
    [Required(ErrorMessage = "Vui lòng nhập mã đơn hàng.")]
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Tên / mô tả đơn (orderInfo).</summary>
    [Required(ErrorMessage = "Vui lòng nhập tên đơn hàng.")]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Số tiền thanh toán (VND).</summary>
    [Range(1, long.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0.")]
    public decimal Amount { get; set; }

    /// <summary>Chuỗi tùy chọn (ví dụ Guid phiên checkout) — gửi kèm MoMo và nhận lại ở callback.</summary>
    public string? ExtraData { get; set; }
}
