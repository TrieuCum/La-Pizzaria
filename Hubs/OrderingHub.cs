using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace LaPizzaria.Hubs
{
    public class OrderingHub : Hub
    {
        public async Task BroadcastTableStatusChange(string tableCode)
        {
            await Clients.All.SendAsync("tableStatusChanged", tableCode);
        }

        public async Task BroadcastOrderUpdated(int orderId)
        {
            await Clients.All.SendAsync("orderUpdated", orderId);
        }
    }
}


