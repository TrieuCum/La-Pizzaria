namespace LaPizzaria.ViewModels
{
    public class OrderManagementIndexViewModel
    {
        public IEnumerable<LaPizzaria.Models.Order> Orders { get; set; } = new List<LaPizzaria.Models.Order>();
        public string? Search { get; set; }
        public string? StatusFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

        /// <summary>Số đơn theo từng trạng thái (để hiển thị trên nút lọc).</summary>
        public Dictionary<string, int> StatusCounts { get; set; } = new Dictionary<string, int>();
    }
}
