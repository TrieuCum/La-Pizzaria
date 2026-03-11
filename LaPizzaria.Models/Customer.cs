using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaPizzaria.Models
{
    public class Customer
    {
        // Khóa chính
        [Key]
        public int Id { get; set; }

        // Ánh xạ đúng với bảng Customers trong DB bạn bè gửi
        // (UserId, CustomerCode, LoyaltyPoints, Status, CreatedAt, UpdatedAt)

        [Required]
        [StringLength(900)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CustomerCode { get; set; }

        public int LoyaltyPoints { get; set; }

        [StringLength(100)]
        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Quan hệ tới AspNetUsers (ApplicationUser) qua UserId
        public ApplicationUser? User { get; set; }

        // Các thuộc tính cũ dùng cho giao diện trước đây,
        // không tồn tại trong DB của bạn bè nên đánh dấu NotMapped

        [NotMapped]
        public string? FullName { get; set; }

        [NotMapped]
        public string? Phone { get; set; }

        [NotMapped]
        [EmailAddress]
        public string? Email { get; set; }

        [NotMapped]
        public int Points { get; set; } = 0;

        /// <summary> Số đơn hàng của khách (từ Orders.UserId), không lưu DB. </summary>
        [NotMapped]
        public int OrderCount { get; set; }
    }
}