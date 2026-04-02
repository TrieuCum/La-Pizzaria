using System;
using System.Collections.Generic;
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
			ViewBag.WindowTimeStart = FormatMinute(v.WindowTimeStartMinute);
			ViewBag.WindowTimeEnd = FormatMinute(v.WindowTimeEndMinute);
			ViewBag.WeekDaysSelected = string.IsNullOrEmpty(v.ValidDaysOfWeek)
				? new HashSet<int>()
				: v.ValidDaysOfWeek.Split(',').Select(s => int.TryParse(s.Trim(), out var x) ? x : -1).Where(x => x >= 0 && x <= 6).ToHashSet();
			return View(v);
		}

		private static string? FormatMinute(int? m)
		{
			if (m == null) return null;
			var x = Math.Clamp(m.Value, 0, 1439);
			return $"{x / 60:D2}:{x % 60:D2}";
		}

		private static int? ParseTimeToMinute(string? s)
		{
			if (string.IsNullOrWhiteSpace(s)) return null;
			var p = s.Trim().Split(':', StringSplitOptions.RemoveEmptyEntries);
			if (p.Length < 2) return null;
			if (!int.TryParse(p[0], out var h) || !int.TryParse(p[1], out var min)) return null;
			var total = h * 60 + min;
			if (total < 0 || total > 1439) return null;
			return total;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Upsert(Voucher model, string? windowTimeStart, string? windowTimeEnd, [FromForm] int[]? weekDays)
		{
			model.WindowTimeStartMinute = ParseTimeToMinute(windowTimeStart);
			model.WindowTimeEndMinute = ParseTimeToMinute(windowTimeEnd);
			model.ValidDaysOfWeek = weekDays != null && weekDays.Length > 0
				? string.Join(",", weekDays.Distinct().Where(d => d >= 0 && d <= 6).OrderBy(d => d))
				: null;

			if (!ModelState.IsValid) 
            {
                ViewBag.Users = await _db.Users.OrderBy(u => u.Email).ToListAsync();
                ViewBag.WindowTimeStart = windowTimeStart;
                ViewBag.WindowTimeEnd = windowTimeEnd;
                ViewBag.WeekDaysSelected = weekDays != null ? weekDays.ToHashSet() : new HashSet<int>();
                return View(model);
            }
			if (model.Id == 0)
			{
				model.UsedCount = 0;
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
				existing.WindowTimeStartMinute = model.WindowTimeStartMinute;
				existing.WindowTimeEndMinute = model.WindowTimeEndMinute;
				existing.ValidDaysOfWeek = model.ValidDaysOfWeek;
				existing.UpsaleRequiresSlowSeller = model.UpsaleRequiresSlowSeller;
				existing.UpdatedAtUtc = DateTime.UtcNow;
				await _svc.UpdateAsync(existing);
			}
			TempData["success"] = "Lưu voucher thành công";
			return RedirectToAction(nameof(Index));
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
				remainingSeconds = _svc.TimeRemaining(v, now)?.TotalSeconds
			});
			return Ok(list);
		}
	}
}


