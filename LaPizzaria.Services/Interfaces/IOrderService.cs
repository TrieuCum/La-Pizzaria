using System.Collections.Generic;
using System.Threading.Tasks;
using LaPizzaria.Models;

namespace LaPizzaria.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(string? userId, IEnumerable<OrderDetail> items, IEnumerable<int> tableIds, double? latitude = null, double? longitude = null);
        Task<bool> MergeTablesAsync(int targetOrderId, IEnumerable<int> sourceTableIds);
        Task<List<Order>> SplitOrderAsync(int orderId, Dictionary<int, int> orderDetailIdToNewQuantity);
        Task<decimal> CalculateTotalAsync(int orderId);
        Task<bool> AssignTablesAsync(int orderId, IEnumerable<int> tableIds);
    }
}


