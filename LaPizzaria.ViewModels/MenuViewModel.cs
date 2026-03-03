using LaPizzaria.Models;

namespace LaPizzaria.ViewModels
{
    public class MenuViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Combo> Combos { get; set; } = new List<Combo>();
    }
}
