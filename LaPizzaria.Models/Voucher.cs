using System;
using System.Collections.Generic;

namespace LaPizzaria.Models
{
	public class Voucher
	{
		public int Id { get; set; }
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string VoucherType { get; set; } = "Percentage";
		public decimal DiscountPercent { get; set; }
		public decimal DiscountAmount { get; set; }
		public decimal MinOrderValue { get; set; }

		public string? TargetUserId { get; set; }
		public ApplicationUser? TargetUser { get; set; }

		public int MaxUses { get; set; }
		public int UsedCount { get; set; }
		public DateTime? StartsAt { get; set; }
		public DateTime? ExpiresAt { get; set; }
		public string? ValidDaysOfWeek { get; set; }
		public int? TimeStartMinute { get; set; }
		public int? TimeEndMinute { get; set; }

		public int? TargetProductId { get; set; }
		public Product? TargetProduct { get; set; }

		public bool UpsaleRequiresSlowSeller { get; set; }
		public bool IsActive { get; set; } = true;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

		public ICollection<VoucherProduct> VoucherProducts { get; set; } = new List<VoucherProduct>();
	}
}
