using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MarketPlaceApi.Hubs
{
    public class NotificationHub : Hub
    {
        private const string AdminEmail = "admin@marketplace.com";

        public override async Task OnConnectedAsync()
        {
            var userEmail = Context.GetHttpContext().Request.Query["email"].ToString();

            if (userEmail == AdminEmail)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
            await base.OnDisconnectedAsync(exception);
        }
    }
}