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

        public async Task SendNotificationToReading(string message, string resident_id)
        {
            var hubConnections = await _connectionRepository.SelectHubConnection(resident_id);
            foreach (var hubConnection in hubConnections)
            {
                await Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedReadingNotification", message, resident_id);
            }
        }

        public async Task SendNotificationToBilling(string message, string resident_id)
        {
            var hubConnections = await _connectionRepository.SelectHubConnection(resident_id);
            foreach (var hubConnection in hubConnections)
            {
                await Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedBillingNotification", message, resident_id);
            }
        }

        public async Task SendNotificationToAddress(string message, string resident_id)
        {
            var hubConnections = await _connectionRepository.SelectHubConnection(resident_id);
            foreach (var hubConnection in hubConnections)
            {
                await Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedAddressNotification", message, resident_id);
            }
        }

        public async Task SendNotificationToMyAddress(string message, string resident_id)
        {
            //var hubConnections = await _connectionRepository.SelectHubConnection(resident_id);
            //foreach (var hubConnection in hubConnections)
            //{
            //    await Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedAddressNotificationToAdmin", message, resident_id);
            //}
            await Clients.All.SendAsync("ReceivedMyAddressNotification", message);

        }

        public async Task SendNotificationToRequestPage(string message, string resident_id)
        {
            //var hubConnections = await _connectionRepository.SelectHubConnection(resident_id);
            //foreach (var hubConnection in hubConnections)
            //{
            //    await Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedAddressNotificationToAdmin", message, resident_id);
            //}
            await Clients.All.SendAsync("ReceivedRequestPageNotificationToMyAddress", message);

        }
        #region FOR ADMIN
        public async Task SendNotificationToAdminAddressRegister(string message, string resident_id)
        {
            //var hubConnections = await _connectionRepository.SelectHubConnection(resident_id);
            //foreach (var hubConnection in hubConnections)
            //{
            //    await Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedAddressNotificationToAdmin", message, resident_id);
            //}
            await Clients.All.SendAsync("ReceivedAddressNotificationToAdmin", message);

        }

        public async Task SendNotificationToAdminRequestPage(string message, string resident_id)
        {
            //var hubConnections = await _connectionRepository.SelectHubConnection(resident_id);
            //foreach (var hubConnection in hubConnections)
            //{
            //    await Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedAddressNotificationToAdmin", message, resident_id);
            //}
            await Clients.All.SendAsync("ReceivedRequestPageNotificationToAdmin", message);

        }
        #endregion


        public async Task SaveUserConnection(string resident_id, string username)
        {
            var userIdentifier = Context.UserIdentifier;
            var connectionID = Context.ConnectionId;
            HubConnect hubConnection = new HubConnect
            {
                Connection_ID = connectionID,
                Resident_ID = resident_id,
                Username = username
            };

            await _connectionRepository.SaveHubConnection(hubConnection);
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("OnConnected");
            await base.OnConnectedAsync();
            //return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            Task.Run(async () => await _connectionRepository.RemoveHubConnection(connectionId));

            return base.OnDisconnectedAsync(exception);
        }

        public async Task ClientDisconnecting()
        {
            var connectionId = Context.ConnectionId;
            await _connectionRepository.RemoveHubConnection(connectionId);
        }
    }
}
