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
			var now = DateTime.UtcNow;
			return await _db.Vouchers
				.Where(v => v.IsActive && (v.ExpiresAtUtc == null || v.ExpiresAtUtc > now) && (v.MaxUses == 0 || v.UsedCount < v.MaxUses))
				.OrderBy(v => v.ExpiresAtUtc)
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

		public bool IsUsable(Voucher v, DateTime nowUtc)
		{
			if (!v.IsActive) return false;
			if (v.ExpiresAtUtc != null && v.ExpiresAtUtc <= nowUtc) return false;
			if (v.MaxUses > 0 && v.UsedCount >= v.MaxUses) return false;
			if (v.DiscountPercent <= 0) return false;
			return true;
		}

		public TimeSpan? TimeRemaining(Voucher v, DateTime nowUtc)
		{
			if (v.ExpiresAtUtc == null) return null;
			var span = v.ExpiresAtUtc.Value - nowUtc;
			if (span <= TimeSpan.Zero) return TimeSpan.Zero;
			return span;
		}
	}
}


