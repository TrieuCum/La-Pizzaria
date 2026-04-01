using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Services
{
	public class VoucherService : IVoucherService
	{
		private readonly ApplicationDbContext _db;
		public VoucherService(ApplicationDbContext db)
		{
			_db = db;
		}

		public async Task<List<Voucher>> ListActiveAsync()
		{
			var now = DateTime.Now;
			return await _db.Vouchers
				.Include(v => v.TargetProduct)
				.Where(v => v.IsActive && (v.ExpiresAt == null || v.ExpiresAt > now) && (v.MaxUses == 0 || v.UsedCount < v.MaxUses))
				.OrderBy(v => v.ExpiresAt)
				.ToListAsync();
		}

		public async Task<Voucher?> GetByCodeAsync(string code)
		{
			var c = code.Trim();
			return await _db.Vouchers.FirstOrDefaultAsync(v => v.Code == c);
		}

		public async Task<Voucher?> GetByIdAsync(int id)
		{
			return await _db.Vouchers.FindAsync(id);
		}

		public async Task<Voucher> CreateAsync(Voucher v)
		{
			_db.Vouchers.Add(v);
			await _db.SaveChangesAsync();
			return v;
		}

		public async Task<Voucher> UpdateAsync(Voucher v)
		{
			_db.Vouchers.Update(v);
			await _db.SaveChangesAsync();
			return v;
		}

		public async Task<bool> DeleteAsync(int id)
		{
			var v = await _db.Vouchers.FindAsync(id);
			if (v == null) return false;
			_db.Vouchers.Remove(v);
			await _db.SaveChangesAsync();
			return true;
		}

		public bool IsUsable(Voucher v, DateTime now)
		{
			if (!v.IsActive) return false;
			if (v.StartsAt != null && v.StartsAt > now) return false;
			if (v.ExpiresAt != null && v.ExpiresAt <= now) return false;
			if (v.MaxUses > 0 && v.UsedCount >= v.MaxUses) return false;
			
            // Use current local time for recurring checks
            var currentTime = now.TimeOfDay;
            
            // Check days of week
            if (!string.IsNullOrEmpty(v.ValidDaysOfWeek))
            {
                var day = ((int)now.DayOfWeek).ToString(); // 0=Sunday, 1=Monday...
                var validDays = v.ValidDaysOfWeek.Split(',');
                if (!validDays.Contains(day)) return false;
            }

            // Check specific time of day (Recurring)
            if (v.StartTime.HasValue && currentTime < v.StartTime.Value) return false;
            if (v.EndTime.HasValue && currentTime > v.EndTime.Value) return false;

            // Basic validity check
            if (v.VoucherType == "Percentage" && v.DiscountPercent <= 0) return false;
            if (v.VoucherType == "FixedAmount" && v.DiscountAmount <= 0) return false;

			return true;
		}

		public bool IsUsable(Voucher v, DateTime now, List<OrderDetail> details)
		{
			if (!IsUsable(v, now)) return false;
			if (v.TargetProductId.HasValue)
			{
				return details.Any(d => d.ProductId == v.TargetProductId.Value || d.ProductId2 == v.TargetProductId.Value);
			}
			return true;
		}

		public TimeSpan? TimeRemaining(Voucher v, DateTime now)
		{
			if (v.ExpiresAt == null) return null;
			var span = v.ExpiresAt.Value - now;
			if (span <= TimeSpan.Zero) return TimeSpan.Zero;
			return span;
		}

		public decimal CalculateDiscount(Voucher v, List<OrderDetail> details, decimal subtotal)
		{
			// If a target product is required, check if it exists in the cart
			if (v.TargetProductId.HasValue)
			{
				bool hasTarget = details.Any(d => d.ProductId == v.TargetProductId.Value || d.ProductId2 == v.TargetProductId.Value);
				if (!hasTarget) return 0; // Condition not met
			}

			if (subtotal <= 0) return 0;

			if (v.VoucherType == "Percentage")
				return Math.Round(subtotal * (v.DiscountPercent / 100m), 0);
			
			if (v.VoucherType == "FixedAmount" || v.VoucherType == "FreeShipping")
			{
				// Limit fixed discount to the subtotal
				return Math.Min(subtotal, v.DiscountAmount);
			}

			return 0;
		}
	}
}


