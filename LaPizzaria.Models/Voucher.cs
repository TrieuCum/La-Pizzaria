using System;
using System.Collections.Generic;

namespace LaPizzaria.Models
{
	public class Voucher
	{
		public int Id { get; set; }
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;

		// Voucher Type: Percentage, FixedAmount, FreeShipping, FreeProduct
		public string VoucherType { get; set; } = "Percentage";

		/// <summary>Phạm vi áp dụng: "All" | "Food" | "Combo" | "Shipping". Null = "All".</summary>
		public string? AppliesTo { get; set; }

		/// <summary>Sản phẩm tặng kèm (chỉ dùng khi VoucherType = "FreeProduct").</summary>
		public int? FreeProductId { get; set; }
		public virtual Product? FreeProduct { get; set; }

		public decimal DiscountPercent { get; set; } // 0-100
		public decimal DiscountAmount { get; set; } // For FixedAmount or Max Discount
		
		public decimal MinOrderValue { get; set; } = 0; // Minimum order to apply
		
		public string? TargetUserId { get; set; } // For specific user
		public virtual ApplicationUser? TargetUser { get; set; }

		public int MaxUses { get; set; } = 0; // 0 => unlimited
		public int UsedCount { get; set; } = 0;
		public DateTime? ExpiresAtUtc { get; set; }
		public bool IsActive { get; set; } = true;
		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

		/// <summary>Phút trong ngày (0–1439) giờ VN — bắt đầu khung giờ. Null = không giới hạn giờ.</summary>
		public int? WindowTimeStartMinute { get; set; }
		/// <summary>Phút trong ngày kết thúc khung (có thể &lt; start nếu qua nửa đêm).</summary>
		public int? WindowTimeEndMinute { get; set; }
		/// <summary>Ngày áp dụng: "0,1,…,6" — 0=CN … 6=Thứ 7 (DayOfWeek). Rỗng/null = mọi ngày.</summary>
		public string? ValidDaysOfWeek { get; set; }
		/// <summary>Chỉ áp dụng khi giỏ có ít nhất một sản phẩm <see cref="Product.IsSlowSeller"/>.</summary>
		public bool UpsaleRequiresSlowSeller { get; set; }
		public ICollection<VoucherProduct> VoucherProducts { get; set; } = new List<VoucherProduct>();
	}
}


