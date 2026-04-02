namespace LaPizzaria.ViewModels
{
    public class OrderHistoryViewModel
    {
        public IEnumerable<LaPizzaria.Models.Order> Orders { get; set; } = new List<LaPizzaria.Models.Order>();
        public string? StatusFilter { get; set; }
        public int AllCount { get; set; }
        public int DeliveringCount { get; set; }
        public int CompletedCount { get; set; }
        public int RatedCount { get; set; }
        public int CancelledCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}
