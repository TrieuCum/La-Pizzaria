using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LaPizzaria.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LaPizzaria.Services
{
	public class VoucherCleanupService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		public VoucherCleanupService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// initial delay to let app start
			await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using var scope = _serviceProvider.CreateScope();
					var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
					var now = DateTime.UtcNow;
					var toRemove = await db.Vouchers
						.Where(v => !v.IsActive || (v.ExpiresAtUtc != null && v.ExpiresAtUtc <= now) || (v.MaxUses > 0 && v.UsedCount >= v.MaxUses))
						.ToListAsync(stoppingToken);
					if (toRemove.Count > 0)
					{
						// remove mapping rows first
						var ids = toRemove.Select(v => v.Id).ToList();
						var mappings = db.OrderVouchers.Where(ov => ids.Contains(ov.VoucherId));
						db.OrderVouchers.RemoveRange(mappings);
						db.Vouchers.RemoveRange(toRemove);
						await db.SaveChangesAsync(stoppingToken);
					}
				}
				catch
				{
					// swallow errors to keep service alive; consider logging in real app
				}
				await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
			}
		}
	}
}


