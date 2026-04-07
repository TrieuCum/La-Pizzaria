using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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
		public DateTime? ExpiresAtUtc { get; set; }
		public bool IsActive { get; set; } = true;
		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
		public int? WindowTimeStartMinute { get; set; }
		public int? WindowTimeEndMinute { get; set; }
		public string? ValidDaysOfWeek { get; set; }
		public bool UpsaleRequiresSlowSeller { get; set; }

		public ICollection<VoucherProduct> VoucherProducts { get; set; } = new List<VoucherProduct>();

		[NotMapped]
		public DateTime? StartsAt { get; set; }

		[NotMapped]
		public DateTime? ExpiresAt
		{
			get => ExpiresAtUtc;
			set => ExpiresAtUtc = value;
		}

		[NotMapped]
		public DateTime CreatedAt
		{
			get => CreatedAtUtc;
			set => CreatedAtUtc = value;
		}

		[NotMapped]
		public DateTime UpdatedAt
		{
			get => UpdatedAtUtc;
			set => UpdatedAtUtc = value;
		}

		[NotMapped]
		public int? TimeStartMinute
		{
			get => WindowTimeStartMinute;
			set => WindowTimeStartMinute = value;
		}

		[NotMapped]
		public int? TimeEndMinute
		{
			get => WindowTimeEndMinute;
			set => WindowTimeEndMinute = value;
		}

		[NotMapped]
		public int? TargetProductId { get; set; }

		[NotMapped]
		public Product? TargetProduct { get; set; }
	}
}
