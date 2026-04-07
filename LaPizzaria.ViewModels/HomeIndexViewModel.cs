using System.Collections.Generic;
using LaPizzaria.Models;

namespace LaPizzaria.ViewModels
{
    public class TopProductVm
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int TotalOrdered { get; set; }
        public decimal Price { get; set; }
    }

    public class HomeIndexViewModel
    {
        public List<TopProductVm> TopProducts { get; set; } = new List<TopProductVm>();
        public List<Combo> ActiveCombos { get; set; } = new List<Combo>();
        public List<Voucher> Vouchers { get; set; } = new List<Voucher>();
    }
}


