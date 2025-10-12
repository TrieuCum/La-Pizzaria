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
		TimeSpan? TimeRemaining(Voucher v, DateTime nowUtc);
	}
}


