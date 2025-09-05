using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace server.Hubs
{
    public class OrdersHub : Hub
    {
        public Task SubscribeToOrder(int orderId)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }

        public Task UnsubscribeFromOrder(int orderId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }
    }
}
