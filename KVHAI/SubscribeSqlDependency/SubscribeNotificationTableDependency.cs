using KVHAI.Hubs;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.SignalR;
using TableDependency.SqlClient;

namespace KVHAI.SubscribeSqlDependency
{
    public class SubscribeNotificationTableDependency : ISubscribeTableDependency
    {
        SqlTableDependency<Notification> tableDependency;
        NotificationHub notificationHub;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly HubConnectionRepository _connectionRepository;


        public SubscribeNotificationTableDependency(NotificationHub notificationHub, IHubContext<NotificationHub> hubContext, HubConnectionRepository hubConnectionRepository)
        {
            this.notificationHub = notificationHub;
            _hubContext = hubContext;
            _connectionRepository = hubConnectionRepository;
        }

        public void SubscribeTableDependency(string connectionString)
        {
            tableDependency = new SqlTableDependency<Notification>(connectionString, "notification_tb");
            tableDependency.OnChanged += TableDependency_OnChanged;
            tableDependency.OnError += TableDependency_OnError;
            tableDependency.Start();
        }

        private void TableDependency_OnError(object sender, TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs e)
        {
            Console.WriteLine($"{nameof(Notification)} SqlTableDependency error: {e.Error.Message}");
        }

        private async void TableDependency_OnChanged(object sender, TableDependency.SqlClient.Base.EventArgs.RecordChangedEventArgs<Notification> e)
        {
            if (e.ChangeType != TableDependency.SqlClient.Base.Enums.ChangeType.None)
            {
                var notification = e.Entity;
                if (notification.Message_Type == "All")
                {
                    //await notificationHub.SendNotificationToAll(notification.Message);
                    await _hubContext.Clients.All.SendAsync("ReceivedNotification", notification.Message);
                }
                else if (notification.Message_Type == "Personal")
                {
                    var hubConnections = await _connectionRepository.SelectHubConnection(notification.Resident_ID);

                    if (hubConnections != null && hubConnections.Count > 0)
                    {
                        foreach (var hubConnection in hubConnections)
                        {
                            int res_id = Convert.ToInt32(notification.Resident_ID);

                            await _hubContext.Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedNotification", notification.Message, res_id);
                        }
                    }
                }
            }
        }


    }
}
