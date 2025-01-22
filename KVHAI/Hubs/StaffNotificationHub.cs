using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.SignalR;

namespace KVHAI.Hubs
{
    public class StaffNotificationHub : Hub
    {
        private readonly HubConnectionRepository _connectionRepository;

        public StaffNotificationHub(HubConnectionRepository hubConnectionRepository)
        {
            _connectionRepository = hubConnectionRepository;
        }

        public async Task SendNotificationToAll(string message)
        {
            await Clients.All.SendAsync("ReceivedNotification", message);
        }

        public async Task NotifyWaterReading()
        {
            await Clients.All.SendAsync("ReceivedWaterReading");
        }

        public async Task NotifyNewAccount()
        {
            await Clients.All.SendAsync("ReceivedNewRegisterAccount");
        }

        public async Task SendNotificationToAdminAddressRegister()
        {

            await Clients.All.SendAsync("ReceivedAddressNotificationToAdmin");

        }

        public async Task SendNotificationToAdminDashboard()
        {

            await Clients.All.SendAsync("ReceivedDataDashboard");

        }

        public async Task SendNotificationToAdminRequestPage(string message, string resident_id)
        {
            await Clients.All.SendAsync("ReceivedRequestPageNotificationToAdmin", message);

        }

        public async Task SendNotificationPaymentOnline()
        {
            await Clients.All.SendAsync("ReceiveOnlinePayment");

        }

        public async Task SendNotificationPaymentOffline()
        {
            await Clients.All.SendAsync("ReceiveOfflinePayment");

        }



        public async Task NotifyWaterBilling()
        {
            await Clients.All.SendAsync("ReceivedWaterBilling");
        }

        public async Task NotifyAdmin()
        {
            await Clients.All.SendAsync("ReceivedAdminNotif");
        }

        public async Task SendNotificationToClient(string message, string employee_id)
        {
            var hubConnections = await _connectionRepository.SelectHubConnectionEmployee(employee_id);
            foreach (var hubConnection in hubConnections)
            {
                await Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedPersonalNotification", message, employee_id);
            }
        }

        public async Task SaveUserConnection(string employee_id, string username)
        {
            var userIdentifier = Context.UserIdentifier;
            var connectionID = Context.ConnectionId;
            HubConnect hubConnection = new HubConnect
            {
                Connection_ID = connectionID,
                Employee_ID = employee_id,
                Resident_ID = "0",
                Username = username
            };

            await _connectionRepository.SaveHubConnectionEmployee(hubConnection);
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
            Task.Run(async () => await _connectionRepository.RemoveHubConnectionEmployee(connectionId));

            return base.OnDisconnectedAsync(exception);
        }

        public async Task ClientDisconnecting()
        {
            var connectionId = Context.ConnectionId;
            await _connectionRepository.RemoveHubConnectionEmployee(connectionId);
        }
    }
}
