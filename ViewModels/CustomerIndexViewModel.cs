namespace LaPizzaria.ViewModels
{
    public class CustomerIndexViewModel
    {
        public IEnumerable<LaPizzaria.Models.Customer> Customers { get; set; } = new List<LaPizzaria.Models.Customer>();
        public string? Search { get; set; }
        public string? StatusFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
