namespace LaPizzaria.ViewModels
{
    public class ShipperIndexViewModel
    {
        public ShipperOrderSummaryViewModel? ActiveOrder { get; set; }
        public List<ShipperOrderSummaryViewModel> NewOrders { get; set; } = new();
        public int DeliveredTodayCount { get; set; }
        public decimal IncomeToday { get; set; }
    }

    public class ShipperOrderSummaryViewModel
    {
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public string ItemTitle { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class ShipperOrderDetailViewModel
    {
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<ShipperOrderItemViewModel> Items { get; set; } = new();
    }

    public class ShipperOrderItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public string? Size { get; set; }
    }

public class ShipperHistoryViewModel
{
    public List<ShipperOrderSummaryViewModel> Orders { get; set; } = new();
    public string Status { get; set; } = "All";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public decimal TotalIncome { get; set; }
}

public class ShipperIncomeViewModel
{
    public decimal IncomeToday { get; set; }
    public decimal IncomeThisWeek { get; set; }
    public decimal IncomeThisMonth { get; set; }
    public int CompletedOrdersThisMonth { get; set; }
    public decimal TotalLast7Days { get; set; }
    public decimal MaxChartValue { get; set; }
    public string OrderCodeSearch { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public List<IncomeChartPointViewModel> Last7Days { get; set; } = new();
    public List<ShipperIncomeOrderRowViewModel> RecentCompletedOrders { get; set; } = new();
}

public class IncomeChartPointViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int HeightPercent { get; set; }
    public bool Highlight { get; set; }
}

public class ShipperIncomeOrderRowViewModel
{
    public int OrderId { get; set; }
    public DateTime CompletedAt { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalIncome { get; set; }
    public string DistanceText { get; set; } = "N/A";
    public string StatusLabel { get; set; } = "Da nhan";
    public bool IsCompleted { get; set; } = true;
}

public class ShipperMonthlySummaryViewModel
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalIncome { get; set; }
    public int TotalOrders { get; set; }
    public decimal MaxChartValue { get; set; }
    public List<IncomeChartPointViewModel> DailyChart { get; set; } = new();
    public List<ShipperIncomeOrderRowViewModel> Orders { get; set; } = new();
}

public class ShipperWithdrawViewModel
{
    public string AccountName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public decimal AvailableAmount { get; set; }
}
}
