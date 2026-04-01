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
		public DateTime? StartsAt { get; set; }
		public DateTime? ExpiresAt { get; set; }
		
		// Recurring schedule (Optional)
		public string? ValidDaysOfWeek { get; set; } // e.g. "1,2,3" for Mon, Tue, Wed (0=Sunday)
		public TimeSpan? StartTime { get; set; }
		public TimeSpan? EndTime { get; set; }

		public int? TargetProductId { get; set; }
		public virtual Product? TargetProduct { get; set; }

		public bool IsActive { get; set; } = true;
		public DateTime CreatedAt { get; set; } = DateTime.Now;
		public DateTime UpdatedAt { get; set; } = DateTime.Now;
	}
}


