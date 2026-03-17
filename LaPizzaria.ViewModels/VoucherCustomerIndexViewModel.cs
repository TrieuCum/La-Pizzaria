using LaPizzaria.Models;

namespace LaPizzaria.ViewModels
{
    public class VoucherCustomerIndexViewModel
    {
        public List<Voucher> Vouchers { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<Combo> Combos { get; set; } = new();
        /// <summary>Filter: All, Pizza, Drink, Side Dish, Combo</summary>
        public string CurrentCategory { get; set; } = "All";
        /// <summary>Tab: vouchers | monan | vanchuyen | thanhvien</summary>
        public string CurrentTab { get; set; } = "vouchers";
        public List<CategoryFilterItem> CategoryFilters { get; set; } = new();
        /// <summary>Danh sách Id voucher mà user đã lưu vào tài khoản.</summary>
        public List<int> SavedVoucherIds { get; set; } = new();
        /// <summary>User đã đăng nhập hay chưa (để hiện nút "Lưu mã" / "Đăng nhập để lưu mã").</summary>
        public bool IsAuthenticated { get; set; }
    }

    public class CategoryFilterItem
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
