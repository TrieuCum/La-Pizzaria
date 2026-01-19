using System;
using System.Collections.Generic;

namespace LaPizzaria.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string OrderCode { get; set; } = string.Empty; // Mã đơn hàng
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalPrice { get; set; }
        public decimal Subtotal { get; set; } // Tạm tính
        public decimal ShippingFee { get; set; } = 0; // Phí vận chuyển
        public decimal DiscountAmount { get; set; } = 0; // Giảm giá từ voucher/reward
        public string OrderStatus { get; set; } = "Đã nhận đơn";
        public string DeliveryType { get; set; } = "DineIn"; // DineIn (ăn tại chỗ) hoặc Delivery (giao tận nơi)
        public string? DeliveryAddress { get; set; }
        public string? PaymentMethod { get; set; } // Cash, MoMo
        public string? Notes { get; set; }
        public DateTime? EstimatedDeliveryTime { get; set; } // Thời gian dự kiến giao hàng
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();
		public ICollection<OrderVoucher> OrderVouchers { get; set; } = new List<OrderVoucher>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
