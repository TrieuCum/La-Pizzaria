using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.Services;

namespace LaPizzaria.Controllers
{
	[Authorize(Roles = "Admin,Staff")]
	public class VoucherController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly IVoucherService _svc;
		public VoucherController(ApplicationDbContext db, IVoucherService svc)
		{
			_db = db; _svc = svc;
		}

		public async Task<IActionResult> Index(string? search, string? status)
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

            var list = await query.OrderByDescending(v => v.CreatedAtUtc).ToListAsync();
            ViewBag.Search = search;
            ViewBag.Status = status;
			return View(list);
		}

		public async Task<IActionResult> Upsert(int? id)
		{
            ViewBag.Users = await _db.Users.OrderBy(u => u.Email).ToListAsync();
			if (id == null) return View(new Voucher());
			var v = _db.Vouchers.Find(id);
			if (v == null) return NotFound();
			return View(v);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Upsert(Voucher model)
		{
			if (!ModelState.IsValid) 
            {
                ViewBag.Users = await _db.Users.OrderBy(u => u.Email).ToListAsync();
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
			var list = (await _svc.ListActiveAsync()).Select(v => new {
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


