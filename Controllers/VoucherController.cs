using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.Services;
using LaPizzaria.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Controllers
{
	[Authorize(Roles = "Admin,Staff")]
	public class VoucherController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly IVoucherService _svc;
		private readonly UserManager<ApplicationUser> _userManager;

		public VoucherController(ApplicationDbContext db, IVoucherService svc, UserManager<ApplicationUser> userManager)
		{
			_db = db;
			_svc = svc;
			_userManager = userManager;
		}

		public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 10)
		{
			var query = _db.Vouchers.AsQueryable();

			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(v => v.Code.Contains(search) || v.Name.Contains(search));
			}

			if (!string.IsNullOrWhiteSpace(status))
			{
				var now = DateTime.UtcNow;
				if (status == "Active")
					query = query.Where(v => v.IsActive && (v.ExpiresAt == null || v.ExpiresAt > now));
				else if (status == "Inactive")
					query = query.Where(v => !v.IsActive);
				else if (status == "Expired")
					query = query.Where(v => v.ExpiresAt != null && v.ExpiresAt <= now);
			}

			if (page < 1) page = 1;
			if (pageSize <= 0) pageSize = 10;

			var totalCount = await query.CountAsync();
			var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
			if (totalPages == 0) totalPages = 1;
			if (page > totalPages) page = totalPages;

			var list = await query
				.OrderByDescending(v => v.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			ViewBag.Search = search;
			ViewBag.Status = status;
			ViewBag.Page = page;
			ViewBag.PageSize = pageSize;
			ViewBag.TotalCount = totalCount;
			ViewBag.TotalPages = totalPages;
			return View(list);
		}

		public async Task<IActionResult> Upsert(int? id)
		{
			if (id == null)
			{
				var model = new Voucher();
				await PopulateUpsertViewDataAsync(model);
				return View(model);
			}

			var voucher = await _db.Vouchers.FindAsync(id);
			if (voucher == null) return NotFound();

			await PopulateUpsertViewDataAsync(voucher);
			return View(voucher);
		}

		private static string? FormatMinute(int? minute)
		{
			if (minute == null) return null;
			var value = Math.Clamp(minute.Value, 0, 1439);
			return $"{value / 60:D2}:{value % 60:D2}";
		}

		private static int? ParseTimeToMinute(string? input)
		{
			if (string.IsNullOrWhiteSpace(input)) return null;

			var parts = input.Trim().Split(':', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2) return null;
			if (!int.TryParse(parts[0], out var hour) || !int.TryParse(parts[1], out var minute)) return null;

			var total = hour * 60 + minute;
			return total is < 0 or > 1439 ? null : total;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Upsert(
			Voucher model,
			string? windowTimeStart,
			string? windowTimeEnd,
			[FromForm] int[]? weekDays,
			[FromForm] int[]? eligibleProductIds)
		{
			model.TimeStartMinute = ParseTimeToMinute(windowTimeStart);
			model.TimeEndMinute = ParseTimeToMinute(windowTimeEnd);
			model.ValidDaysOfWeek = weekDays != null && weekDays.Length > 0
				? string.Join(",", weekDays.Distinct().Where(day => day >= 0 && day <= 6).OrderBy(day => day))
				: null;

			var selectedProductIds = eligibleProductIds?
				.Where(id => id > 0)
				.Distinct()
				.ToArray() ?? Array.Empty<int>();

			model.TargetProductId = selectedProductIds.Length == 1 ? selectedProductIds[0] : null;

			if (!ModelState.IsValid)
			{
				await PopulateUpsertViewDataAsync(model, windowTimeStart, windowTimeEnd, weekDays, selectedProductIds);
				return View(model);
			}

			var voucherMissing = false;
			var strategy = _db.Database.CreateExecutionStrategy();

			await strategy.ExecuteAsync(async () =>
			{
				await using var tx = await _db.Database.BeginTransactionAsync();

				if (model.Id == 0)
				{
					model.UsedCount = 0;
					model.UpsaleRequiresSlowSeller = false;
					model.CreatedAt = DateTime.UtcNow;
					model.UpdatedAt = DateTime.UtcNow;
					await _svc.CreateAsync(model);
					await ReplaceVoucherProductsAsync(model.Id, selectedProductIds);
				}
				else
				{
					var existing = await _svc.GetByIdAsync(model.Id);
					if (existing == null)
					{
						voucherMissing = true;
						await tx.RollbackAsync();
						return;
					}

					existing.Code = model.Code;
					existing.Name = model.Name;
					existing.VoucherType = model.VoucherType;
					existing.DiscountPercent = model.DiscountPercent;
					existing.DiscountAmount = model.DiscountAmount;
					existing.MinOrderValue = model.MinOrderValue;
					existing.TargetUserId = model.TargetUserId;
					existing.MaxUses = model.MaxUses;
					existing.StartsAt = model.StartsAt;
					existing.ExpiresAt = model.ExpiresAt;
					existing.ValidDaysOfWeek = model.ValidDaysOfWeek;
					existing.TimeStartMinute = model.TimeStartMinute;
					existing.TimeEndMinute = model.TimeEndMinute;
					existing.TargetProductId = model.TargetProductId;
					existing.IsActive = model.IsActive;
					existing.UpsaleRequiresSlowSeller = false;
					existing.UpdatedAt = DateTime.UtcNow;

					await _svc.UpdateAsync(existing);
					await ReplaceVoucherProductsAsync(existing.Id, selectedProductIds);
				}

				await tx.CommitAsync();
			});

			if (voucherMissing) return NotFound();

			TempData["success"] = "Lưu voucher thành công";
			return RedirectToAction(nameof(Index));
		}

		public async Task<IActionResult> Delete(int id)
		{
			await _svc.DeleteAsync(id);
			TempData["success"] = "Đã xóa voucher";
			return RedirectToAction(nameof(Index));
		}

		[HttpGet("/api/vouchers")]
		[AllowAnonymous]
		public async Task<IActionResult> ApiList()
		{
			var now = DateTime.UtcNow;
			var userId = _userManager.GetUserId(User);
			var vouchers = await _svc.ListActiveAsync();
			vouchers = !string.IsNullOrEmpty(userId)
				? vouchers.Where(v => v.TargetUserId == null || v.TargetUserId == userId).ToList()
				: vouchers.Where(v => v.TargetUserId == null).ToList();
			vouchers = vouchers.Where(v => _svc.MatchesTimeAndDay(v, now)).ToList();

			var list = vouchers.Select(v => new
			{
				id = v.Id,
				code = v.Code,
				name = v.Name,
				type = v.VoucherType,
				percent = v.DiscountPercent,
				amount = v.DiscountAmount,
				minOrderValue = v.MinOrderValue,
				targetProductId = v.TargetProductId,
				targetProductName = v.TargetProduct?.Name,
				isActive = v.IsActive,
				startsAt = v.StartsAt,
				expiresAt = v.ExpiresAt,
				validDaysOfWeek = v.ValidDaysOfWeek,
				timeStartMinute = v.TimeStartMinute,
				timeEndMinute = v.TimeEndMinute,
				maxUses = v.MaxUses,
				used = v.UsedCount,
				remainingSeconds = _svc.TimeRemaining(v, now)?.TotalSeconds
			});

			return Ok(list);
		}

		private async Task PopulateUpsertViewDataAsync(
			Voucher model,
			string? windowTimeStart = null,
			string? windowTimeEnd = null,
			IEnumerable<int>? weekDays = null,
			IEnumerable<int>? selectedProductIds = null)
		{
			ViewBag.Users = await _db.Users.OrderBy(u => u.Email).ToListAsync();
			ViewBag.WindowTimeStart = windowTimeStart ?? FormatMinute(model.TimeStartMinute);
			ViewBag.WindowTimeEnd = windowTimeEnd ?? FormatMinute(model.TimeEndMinute);
			ViewBag.WeekDaysSelected = weekDays != null
				? weekDays.Where(x => x >= 0 && x <= 6).ToHashSet()
				: string.IsNullOrEmpty(model.ValidDaysOfWeek)
					? new HashSet<int>()
					: model.ValidDaysOfWeek.Split(',')
						.Select(s => int.TryParse(s.Trim(), out var x) ? x : -1)
						.Where(x => x >= 0 && x <= 6)
						.ToHashSet();

			var selectedSet = selectedProductIds != null
				? selectedProductIds.Where(id => id > 0).ToHashSet()
				: model.Id == 0
					? new HashSet<int>()
					: await _db.VoucherProducts
						.AsNoTracking()
						.Where(vp => vp.VoucherId == model.Id)
						.Select(vp => vp.ProductId)
						.ToHashSetAsync();

			if (selectedSet.Count == 0 && model.TargetProductId.HasValue)
			{
				selectedSet.Add(model.TargetProductId.Value);
			}

			if (selectedSet.Count == 0 && model.Id != 0 && model.UpsaleRequiresSlowSeller)
			{
				selectedSet = await _db.Products
					.AsNoTracking()
					.Where(p => p.IsSlowSeller)
					.Select(p => p.Id)
					.ToHashSetAsync();
				ViewBag.LegacySlowSellerSelection = true;
			}
			else
			{
				ViewBag.LegacySlowSellerSelection = false;
			}

			ViewBag.ProductTargets = await BuildVoucherProductTargetsAsync(selectedSet);
		}

		private async Task<List<VoucherProductTargetItemViewModel>> BuildVoucherProductTargetsAsync(HashSet<int> selectedProductIds)
		{
			var completedStats = await _db.OrderDetails
				.AsNoTracking()
				.Where(od => od.Order != null && od.Order.OrderStatus == "Completed")
				.GroupBy(od => od.ProductId)
				.Select(g => new
				{
					ProductId = g.Key,
					CompletedOrderCount = g.Select(x => x.OrderId).Distinct().Count(),
					TotalUnitsSold = g.Sum(x => x.Quantity)
				})
				.ToDictionaryAsync(x => x.ProductId, x => new { x.CompletedOrderCount, x.TotalUnitsSold });

			var products = await _db.Products
				.AsNoTracking()
				.Select(p => new VoucherProductTargetItemViewModel
				{
					ProductId = p.Id,
					ProductName = p.Name,
					Category = p.Category,
					Price = p.Price,
					IsActive = p.IsActive,
					IsSlowSeller = p.IsSlowSeller,
					IsSelected = selectedProductIds.Contains(p.Id)
				})
				.ToListAsync();

			foreach (var product in products)
			{
				if (completedStats.TryGetValue(product.ProductId, out var stats))
				{
					product.CompletedOrderCount = stats.CompletedOrderCount;
					product.TotalUnitsSold = stats.TotalUnitsSold;
				}
			}

			var soldProducts = products
				.Where(p => p.TotalUnitsSold > 0 || p.CompletedOrderCount > 0)
				.ToList();

			if (soldProducts.Count > 0)
			{
				var maxUnits = soldProducts.Max(p => p.TotalUnitsSold);
				var minUnits = soldProducts.Min(p => p.TotalUnitsSold);

				foreach (var product in soldProducts)
				{
					if (product.TotalUnitsSold == maxUnits)
					{
						product.IsTopSeller = true;
					}

					if (product.TotalUnitsSold == minUnits)
					{
						product.IsLowSeller = true;
					}
				}
			}

			return products
				.OrderByDescending(p => p.IsSelected)
				.ThenByDescending(p => p.IsTopSeller)
				.ThenByDescending(p => p.TotalUnitsSold)
				.ThenByDescending(p => p.CompletedOrderCount)
				.ThenBy(p => p.ProductName)
				.ToList();
		}

		private async Task ReplaceVoucherProductsAsync(int voucherId, IReadOnlyCollection<int> productIds)
		{
			var existing = await _db.VoucherProducts
				.Where(vp => vp.VoucherId == voucherId)
				.ToListAsync();

			if (existing.Count > 0)
			{
				_db.VoucherProducts.RemoveRange(existing);
				await _db.SaveChangesAsync();
			}

			if (productIds.Count == 0) return;

			var validProductIds = await _db.Products
				.AsNoTracking()
				.Where(p => productIds.Contains(p.Id))
				.Select(p => p.Id)
				.ToListAsync();

			_db.VoucherProducts.AddRange(validProductIds.Select(productId => new VoucherProduct
			{
				VoucherId = voucherId,
				ProductId = productId
			}));
			await _db.SaveChangesAsync();
		}
	}
}
