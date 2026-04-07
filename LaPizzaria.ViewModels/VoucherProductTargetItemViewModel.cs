namespace LaPizzaria.ViewModels
{
    public class VoucherProductTargetItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
        public bool IsSlowSeller { get; set; }
        public int CompletedOrderCount { get; set; }
        public int TotalUnitsSold { get; set; }
        public bool IsSelected { get; set; }
        public bool IsTopSeller { get; set; }
        public bool IsLowSeller { get; set; }
    }
}
