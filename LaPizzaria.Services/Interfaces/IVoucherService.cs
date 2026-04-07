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
		bool IsUsable(Voucher v, DateTime now);
		decimal CalculateDiscount(Voucher v, List<OrderDetail> details, decimal subtotal);
		bool MatchesTimeAndDay(Voucher v, DateTime now);
		TimeSpan? TimeRemaining(Voucher v, DateTime now);
		Task<bool> CanApplyToOrderAsync(Voucher v, IReadOnlyList<int>? cartProductIds, DateTime now, CancellationToken ct = default);
	}
}
