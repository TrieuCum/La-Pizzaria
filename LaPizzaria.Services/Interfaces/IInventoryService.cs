using System.Collections.Generic;
using System.Threading.Tasks;
using LaPizzaria.Models;

namespace LaPizzaria.Services
{
    public interface IInventoryService
    {
        Task<bool> CheckAndReserveAsync(IEnumerable<OrderDetail> orderDetails);
        Task ReleaseAsync(IEnumerable<OrderDetail> orderDetails);
        Task<bool> ConsumeAsync(IEnumerable<OrderDetail> orderDetails);
    }
}


