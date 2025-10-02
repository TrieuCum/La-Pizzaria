using Microsoft.AspNetCore.Mvc;
using LaPizzaria.ViewModels;
using LaPizzaria.Models; // Assuming you might need direct access to models later

namespace LaPizzaria.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            // This will display a list of products
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            ProductViewModel productViewModel = new ProductViewModel();

            if (id == null || id == 0)
            {
                // Create new product
                return View(productViewModel);
            }
            else
            {
                // TODO: Retrieve product from database and map to productViewModel
                // For now, returning an empty view model
                return View(productViewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductViewModel productViewModel)
        {
            if (ModelState.IsValid)
            {
                // TODO: Save product to database
                TempData["success"] = productViewModel.Id == 0 ? "Tạo sản phẩm thành công" : "Cập nhật sản phẩm thành công";
                return RedirectToAction("Index");
            }
            return View(productViewModel);
        }
    }
}
