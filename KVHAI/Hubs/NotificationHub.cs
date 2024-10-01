using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace KVHAI.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly HubConnectionRepository _connectionRepository;

        public NotificationHub(HubConnectionRepository hubConnectionRepository)
        {
            _connectionRepository = hubConnectionRepository;
        }

        public async Task SaveUserConnection(string resident_id, string username)
        {
            var connectionID = Context.ConnectionId;
            HubConnect hubConnection = new HubConnect
            {
                Connection_ID = connectionID,
                Resident_ID = resident_id,
                Username = username
            };

            await _connectionRepository.SaveHubConnection(hubConnection);
        }

        public async Task SendNotificationToAll(string message)
        {
            await Clients.All.SendAsync("ReceivedNotification", message);
        }

        public async Task SendNotificationToClient(string message, string resident_id)
        {
            var hubConnections = await _connectionRepository.SelectHubConnection(resident_id);
            foreach (var hubConnection in hubConnections)
            {
                await Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedPersonalNotification", message, resident_id);
            }
        }

        public override Task OnConnectedAsync()
        {
            Clients.Caller.SendAsync("OnConnected");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            Task.Run(async () => await _connectionRepository.RemoveHubConnection(connectionId));

            return base.OnDisconnectedAsync(exception);
        }
    }
}
