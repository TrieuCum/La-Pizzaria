using System;
using System.Collections.Generic;
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
		/// <summary>Kiểm tra khung giờ/ngày + upsale (món bán chậm) khi áp voucher.</summary>
		Task<bool> CanApplyToOrderAsync(Voucher v, DateTime nowUtc, IReadOnlyList<int>? cartProductIds, System.Threading.CancellationToken cancellationToken = default);
		/// <summary>Khung giờ/ngày (VN) — dùng để lọc danh sách voucher hiển thị.</summary>
		bool MatchesTimeAndDay(Voucher v, DateTime nowUtc);
		TimeSpan? TimeRemaining(Voucher v, DateTime nowUtc);
	}
}


