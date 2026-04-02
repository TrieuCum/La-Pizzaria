using System;

namespace LaPizzaria.Models
{
	public class Voucher
	{
		public int Id { get; set; }
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;

		// Voucher Type: Percentage, FixedAmount, FreeShipping
		public string VoucherType { get; set; } = "Percentage"; 

		public decimal DiscountPercent { get; set; } // 0-100
		public decimal DiscountAmount { get; set; } // For FixedAmount or Max Discount
		
		public decimal MinOrderValue { get; set; } = 0; // Minimum order to apply
		
		public string? TargetUserId { get; set; } // For specific user
		public virtual ApplicationUser? TargetUser { get; set; }

		public int MaxUses { get; set; } = 0; // 0 => unlimited
		public int UsedCount { get; set; } = 0;
		public DateTime? ExpiresAtUtc { get; set; }
		/// <summary>Phút trong ngày (0–1439) theo giờ Việt Nam — bắt đầu khung giờ áp dụng. Null = không giới hạn.</summary>
		public int? TimeStartMinute { get; set; }
		/// <summary>Phút trong ngày (0–1439) theo giờ Việt Nam — kết thúc khung giờ.</summary>
		public int? TimeEndMinute { get; set; }
		/// <summary>Ngày trong tuần áp dụng: "0,1,...,6" (0=CN). Rỗng/null = mọi ngày.</summary>
		public string? ValidDaysOfWeek { get; set; }
		/// <summary>Nếu true: đơn phải có ít nhất một món đánh dấu bán chậm (Product.IsSlowSeller).</summary>
		public bool UpsaleRequiresSlowSeller { get; set; }
		public bool IsActive { get; set; } = true;
		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
	}
}


