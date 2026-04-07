using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
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
			var now = DateTime.UtcNow;
			return await _db.Vouchers
				.Where(v => v.IsActive && (v.ExpiresAtUtc == null || v.ExpiresAtUtc > now) && (v.MaxUses == 0 || v.UsedCount < v.MaxUses))
				.OrderBy(v => v.ExpiresAtUtc)
				.ToListAsync();
		}

		public async Task<Voucher?> GetByCodeAsync(string code)
		{
			var normalized = code.Trim();
			return await _db.Vouchers.FirstOrDefaultAsync(v => v.Code == normalized);
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
			var voucher = await _db.Vouchers.FindAsync(id);
			if (voucher == null) return false;

			_db.Vouchers.Remove(voucher);
			await _db.SaveChangesAsync();
			return true;
		}

		public bool IsUsable(Voucher v, DateTime now)
		{
			var localNow = VietnamTime.NormalizeToVietnamLocal(now);
			if (!v.IsActive) return false;
			if (v.ExpiresAtUtc != null && v.ExpiresAtUtc <= localNow) return false;
			if (v.MaxUses > 0 && v.UsedCount >= v.MaxUses) return false;
			if (v.VoucherType == "Percentage" && v.DiscountPercent <= 0) return false;
			if ((v.VoucherType == "FixedAmount" || v.VoucherType == "FreeShipping") && v.DiscountAmount <= 0) return false;
			return true;
		}

		public TimeSpan? TimeRemaining(Voucher v, DateTime now)
		{
			if (v.ExpiresAtUtc == null) return null;

			var localNow = VietnamTime.NormalizeToVietnamLocal(now);
			var span = v.ExpiresAtUtc.Value - localNow;
			return span <= TimeSpan.Zero ? TimeSpan.Zero : span;
		}

		public bool MatchesTimeAndDay(Voucher v, DateTime now)
		{
			var localNow = VietnamTime.NormalizeToVietnamLocal(now);
			var dow = (int)localNow.DayOfWeek;

			if (!string.IsNullOrWhiteSpace(v.ValidDaysOfWeek))
			{
				var allowedDays = v.ValidDaysOfWeek
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.Select(s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var day) ? day : -1)
					.Where(day => day >= 0 && day <= 6)
					.ToHashSet();

				if (allowedDays.Count > 0 && !allowedDays.Contains(dow))
				{
					return false;
				}
			}

			if (v.WindowTimeStartMinute == null || v.WindowTimeEndMinute == null)
			{
				return true;
			}

			var minuteOfDay = VietnamTime.ToMinuteOfDay(localNow);
			var start = v.WindowTimeStartMinute.Value;
			var end = v.WindowTimeEndMinute.Value;

			return start <= end
				? minuteOfDay >= start && minuteOfDay <= end
				: minuteOfDay >= start || minuteOfDay <= end;
		}

		public decimal CalculateDiscount(Voucher v, List<OrderDetail> details, decimal subtotal)
		{
			if (subtotal <= 0) return 0m;

			if (v.VoucherType == "Percentage")
			{
				return Math.Round(subtotal * (v.DiscountPercent / 100m), 0);
			}

			if (v.VoucherType == "FixedAmount" || v.VoucherType == "FreeShipping")
			{
				return Math.Min(subtotal, v.DiscountAmount);
			}

			return 0m;
		}

		public async Task<bool> CanApplyToOrderAsync(Voucher v, IReadOnlyList<int>? cartProductIds, DateTime now, CancellationToken ct = default)
		{
			if (!IsUsable(v, now)) return false;
			if (!MatchesTimeAndDay(v, now)) return false;

			var productIds = cartProductIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();

			var targetedProductIds = await _db.VoucherProducts
				.AsNoTracking()
				.Where(vp => vp.VoucherId == v.Id)
				.Select(vp => vp.ProductId)
				.ToListAsync(ct);

			if (targetedProductIds.Count > 0)
			{
				if (productIds.Count == 0) return false;
				if (!productIds.Intersect(targetedProductIds).Any()) return false;
			}

			if (v.UpsaleRequiresSlowSeller)
			{
				if (productIds.Count == 0) return false;

				var hasSlowSeller = await _db.Products
					.AsNoTracking()
					.AnyAsync(p => productIds.Contains(p.Id) && p.IsSlowSeller, ct);

				if (!hasSlowSeller) return false;
			}

			return true;
		}
	}
}
