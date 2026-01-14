using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.AspNetCore.Authorization;

namespace LaPizzaria.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, string position, string status)
        {
            var query = _context.Employees.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.FullName.Contains(search) || 
                                        e.Email.Contains(search) || 
                                        e.PhoneNumber.Contains(search) ||
                                        e.EmployeeCode.Contains(search));
            }

            if (!string.IsNullOrEmpty(position))
            {
                query = query.Where(e => e.Position == position);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(e => e.Status == status);
            }

            var employees = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
            ViewBag.Positions = await _context.Employees.Select(e => e.Position).Distinct().ToListAsync();
            return View(employees);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Employee { HireDate = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.CreatedAt = DateTime.Now;
                employee.UpdatedAt = DateTime.Now;
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
                TempData["success"] = "Thêm nhân viên thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.UpdatedAt = DateTime.Now;
                _context.Update(employee);
                await _context.SaveChangesAsync();
                TempData["success"] = "Cập nhật nhân viên thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                TempData["success"] = "Xóa nhân viên thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
