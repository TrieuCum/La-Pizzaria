using LaPizzaria.Models;

namespace LaPizzaria.ViewModels
{
    public class MenuViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Combo> Combos { get; set; } = new List<Combo>();
        /// <summary>Từ khóa tìm kiếm (tên/mô tả món, combo)</summary>
        public string? Search { get; set; }
    }
}
