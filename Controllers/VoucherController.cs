using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.Services;
using Microsoft.AspNetCore.Identity;

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
			_db = db; _svc = svc; _userManager = userManager;
		}

		public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 10)
		{
			var query = _db.Vouchers.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v => v.Code.Contains(search) || v.Name.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.UtcNow;
                if (status == "Active")
                    query = query.Where(v => v.IsActive && (v.ExpiresAtUtc == null || v.ExpiresAtUtc > now));
                else if (status == "Inactive")
					query = query.Where(v => !v.IsActive);
                else if (status == "Expired")
                    query = query.Where(v => v.ExpiresAtUtc != null && v.ExpiresAtUtc <= now);
            }

			if (page < 1) page = 1;
			if (pageSize <= 0) pageSize = 10;

			var totalCount = await query.CountAsync();
			var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
			if (totalPages == 0) totalPages = 1;
			if (page > totalPages) page = totalPages;

            var list = await query
				.OrderByDescending(v => v.CreatedAtUtc)
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
            ViewBag.Users = await _db.Users.OrderBy(u => u.Email).ToListAsync();
			if (id == null) return View(new Voucher());
			var v = _db.Vouchers.Find(id);
			if (v == null) return NotFound();
			ViewBag.WindowTimeStart = FormatMinutes(v.TimeStartMinute);
			ViewBag.WindowTimeEnd = FormatMinutes(v.TimeEndMinute);
			return View(v);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Upsert(Voucher model, string? windowTimeStart, string? windowTimeEnd, string? validDaysOfWeek)
		{
			model.TimeStartMinute = ParseTimeToMinutes(windowTimeStart);
			model.TimeEndMinute = ParseTimeToMinutes(windowTimeEnd);
			model.ValidDaysOfWeek = string.IsNullOrWhiteSpace(validDaysOfWeek) ? null : validDaysOfWeek.Trim();

			if (!ModelState.IsValid) 
            {
                ViewBag.Users = await _db.Users.OrderBy(u => u.Email).ToListAsync();
				ViewBag.WindowTimeStart = windowTimeStart;
				ViewBag.WindowTimeEnd = windowTimeEnd;
                return View(model);
            }
			if (model.Id == 0)
			{
				model.UsedCount = 0;
				model.CreatedAtUtc = DateTime.UtcNow;
				model.UpdatedAtUtc = DateTime.UtcNow;
				await _svc.CreateAsync(model);
			}
			else
			{
				var existing = await _svc.GetByIdAsync(model.Id);
				if (existing == null) return NotFound();
				existing.Code = model.Code;
				existing.Name = model.Name;
                existing.VoucherType = model.VoucherType;
				existing.DiscountPercent = model.DiscountPercent;
                existing.DiscountAmount = model.DiscountAmount;
                existing.MinOrderValue = model.MinOrderValue;
                existing.TargetUserId = model.TargetUserId;
				existing.MaxUses = model.MaxUses;
				existing.ExpiresAtUtc = model.ExpiresAtUtc;
				existing.IsActive = model.IsActive;
				existing.TimeStartMinute = model.TimeStartMinute;
				existing.TimeEndMinute = model.TimeEndMinute;
				existing.ValidDaysOfWeek = model.ValidDaysOfWeek;
				existing.UpsaleRequiresSlowSeller = model.UpsaleRequiresSlowSeller;
				existing.UpdatedAtUtc = DateTime.UtcNow;
				await _svc.UpdateAsync(existing);
			}
			TempData["success"] = "Lưu voucher thành công";
			return RedirectToAction(nameof(Index));
		}

		private static int? ParseTimeToMinutes(string? hhmm)
		{
			if (string.IsNullOrWhiteSpace(hhmm)) return null;
			var parts = hhmm.Trim().Split(':');
			if (parts.Length < 2) return null;
			if (!int.TryParse(parts[0], out var h) || !int.TryParse(parts[1], out var m)) return null;
			h = Math.Clamp(h, 0, 23);
			m = Math.Clamp(m, 0, 59);
			return h * 60 + m;
		}

		private static string? FormatMinutes(int? minutes)
		{
			if (minutes == null) return null;
			var v = minutes.Value;
			if (v < 0 || v > 1439) return null;
			return $"{v / 60:D2}:{v % 60:D2}";
		}

		public async Task<IActionResult> Delete(int id)
		{
			await _svc.DeleteAsync(id);
			TempData["success"] = "Đã xoá voucher";
			return RedirectToAction(nameof(Index));
		}

		// API: list active vouchers for selection on QR page
		[HttpGet("/api/vouchers")]
		[AllowAnonymous]
		public async Task<IActionResult> ApiList()
		{
			var now = DateTime.UtcNow;
            var userId = _userManager.GetUserId(User);
            var vouchers = await _svc.ListActiveAsync();
            vouchers = (!string.IsNullOrEmpty(userId))
                ? vouchers.Where(v => v.TargetUserId == null || v.TargetUserId == userId).ToList()
                : vouchers.Where(v => v.TargetUserId == null).ToList();
			vouchers = vouchers.Where(v => _svc.MatchesTimeAndDay(v, now)).ToList();

			var list = vouchers.Select(v => new {
				id = v.Id,
				code = v.Code,
				name = v.Name,
                type = v.VoucherType,
				percent = v.DiscountPercent,
                amount = v.DiscountAmount,
                minOrderValue = v.MinOrderValue,
				maxUses = v.MaxUses,
				used = v.UsedCount,
				expiresAtUtc = v.ExpiresAtUtc,
				remainingSeconds = _svc.TimeRemaining(v, now)?.TotalSeconds,
				requiresUpsaleSlow = v.UpsaleRequiresSlowSeller
			});
			return Ok(list);
		}
	}
}


