using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LaPizzaria.Models;

namespace LaPizzaria.Services
{
	public interface IVoucherService
	{
		Task<List<Voucher>> ListActiveAsync();
		Task<Voucher?> GetByCodeAsync(string code);
		Task<Voucher?> GetByIdAsync(int id);
		Task<Voucher> CreateAsync(Voucher v);
		Task<Voucher> UpdateAsync(Voucher v);
		Task<bool> DeleteAsync(int id);
		bool IsUsable(Voucher v, DateTime nowUtc);
		TimeSpan? TimeRemaining(Voucher v, DateTime nowUtc);
		/// <summary>Khung giờ/ngày (VN) + upsale món bán chậy.</summary>
		bool MatchesTimeAndDay(Voucher v, DateTime utcNow);
		Task<bool> CanApplyToOrderAsync(Voucher v, IReadOnlyList<int> cartProductIds, DateTime utcNow, CancellationToken ct = default);
	}
}
