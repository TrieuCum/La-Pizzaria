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
		public bool IsActive { get; set; } = true;
		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
	}
}


