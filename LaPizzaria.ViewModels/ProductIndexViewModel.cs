using System.Collections.Generic;
using LaPizzaria.Models;

namespace LaPizzaria.ViewModels
{
    public class ProductIndexViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Combo> Combos { get; set; } = new List<Combo>();
        public List<int> OutOfStockIds { get; set; } = new List<int>();
    }
}


