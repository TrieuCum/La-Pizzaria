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

        // Shipper joins their personal group so the server can push targeted notifications
        public async Task JoinShipperGroup(string shipperId)
        {
            if (!string.IsNullOrWhiteSpace(shipperId))
                await Groups.AddToGroupAsync(Context.ConnectionId, $"shipper_{shipperId}");
        }

        public async Task LeaveShipperGroup(string shipperId)
        {
            if (!string.IsNullOrWhiteSpace(shipperId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"shipper_{shipperId}");
        }
    }
}


