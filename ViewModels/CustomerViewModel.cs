using LaPizzaria.Models;

namespace LaPizzaria.ViewModels
{
    public class CustomerViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }

    public class CustomerDetailViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalReviews { get; set; }
        public List<Product> FavoriteProducts { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
    }
}
