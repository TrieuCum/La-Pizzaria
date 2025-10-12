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
	[Authorize(Roles = "Admin")]
	public class VoucherController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly IVoucherService _svc;
		public VoucherController(ApplicationDbContext db, IVoucherService svc)
		{
			_db = db; _svc = svc;
		}

		public async Task<IActionResult> Index()
		{
			var list = await _db.Vouchers.OrderBy(v => v.ExpiresAtUtc).ToListAsync();
			return View(list);
		}

		public IActionResult Upsert(int? id)
		{
			if (id == null) return View(new Voucher());
			var v = _db.Vouchers.Find(id);
			if (v == null) return NotFound();
			return View(v);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Upsert(Voucher model)
		{
			if (!ModelState.IsValid) return View(model);
			if (model.Id == 0)
			{
				model.UsedCount = 0; // newly created voucher starts unused
				await _svc.CreateAsync(model);
			}
			else
			{
				// preserve UsedCount when editing; update only allowed fields
				var existing = await _svc.GetByIdAsync(model.Id);
				if (existing == null) return NotFound();
				existing.Code = model.Code;
				existing.Name = model.Name;
				existing.DiscountPercent = model.DiscountPercent;
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
				percent = v.DiscountPercent,
				maxUses = v.MaxUses,
				used = v.UsedCount,
				expiresAtUtc = v.ExpiresAtUtc,
				remainingSeconds = _svc.TimeRemaining(v, now)?.TotalSeconds
			});
			return Ok(list);
		}
	}
}


