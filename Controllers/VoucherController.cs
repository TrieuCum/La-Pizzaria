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
                var now = DateTime.Now;
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
			var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
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
            ViewBag.Users = await _db.Users.OrderBy(u => u.Email).ToListAsync();
            ViewBag.Products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
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
                ViewBag.Products = await _db.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
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
                existing.StartsAt = model.StartsAt;
				existing.ExpiresAt = model.ExpiresAt;
                existing.ValidDaysOfWeek = model.ValidDaysOfWeek;
                existing.StartTime = model.StartTime;
                existing.EndTime = model.EndTime;
                existing.TargetProductId = model.TargetProductId;
				existing.IsActive = model.IsActive;
				existing.UpdatedAt = DateTime.Now;
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
			var now = DateTime.Now;
            var userId = _userManager.GetUserId(User);
            var vouchers = await _svc.ListActiveAsync();
            vouchers = (!string.IsNullOrEmpty(userId))
                ? vouchers.Where(v => v.TargetUserId == null || v.TargetUserId == userId).ToList()
                : vouchers.Where(v => v.TargetUserId == null).ToList();

			var list = vouchers.Select(v => new {
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
				maxUses = v.MaxUses,
				used = v.UsedCount,
				expiresAt = v.ExpiresAt,
				validDaysOfWeek = v.ValidDaysOfWeek,
				startTime = v.StartTime,
				endTime = v.EndTime,
				remainingSeconds = _svc.TimeRemaining(v, now)?.TotalSeconds
			});
			return Ok(list);
		}
	}
}


