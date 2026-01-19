using System;
using System.Collections.Generic;

namespace LaPizzaria.Models
{
    public class Review
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public int? ProductId { get; set; } // Optional: đánh giá sản phẩm cụ thể
        public int Rating { get; set; } // 1-5 sao
        public string? Comment { get; set; }
        public string? ImageUrl { get; set; } // Hình ảnh đính kèm
        public string? AdminReply { get; set; } // Phản hồi từ admin
        public DateTime? AdminReplyDate { get; set; }
        public int PointsAwarded { get; set; } = 0; // Điểm thưởng sau khi đánh giá
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Order Order { get; set; } = null!;
        public Product? Product { get; set; }
    }
}
