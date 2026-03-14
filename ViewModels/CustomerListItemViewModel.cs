namespace LaPizzaria.ViewModels
{
    /// <summary>Một dòng trong danh sách quản lý khách hàng: user (không phải Admin/Staff) + dữ liệu từ bảng Customers nếu có.</summary>
    public class CustomerListItemViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int Points { get; set; }
        public string? Status { get; set; }
        public int OrderCount { get; set; }
        /// <summary>Id bản ghi Customers (null nếu chưa có).</summary>
        public int? CustomerId { get; set; }
    }
}
