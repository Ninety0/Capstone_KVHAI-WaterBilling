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
        private readonly IHubContext<NotificationHub> _hubContextNotification;
        private readonly IHubContext<WaterBillingHub> _hubContextBilling;
        private readonly IHubContext<WaterReadingHub> _hubContextReading;
        private readonly HubConnectionRepository _connectionRepository;


        public SubscribeNotificationTableDependency(NotificationHub notificationHub, IHubContext<NotificationHub> hubContext, HubConnectionRepository hubConnectionRepository, IHubContext<WaterBillingHub> hubContextBilling, IHubContext<WaterReadingHub> hubContextReading)
        {
            this.notificationHub = notificationHub;
            _hubContextNotification = hubContext;
            _connectionRepository = hubConnectionRepository;
            _hubContextBilling = hubContextBilling;
            _hubContextReading = hubContextReading;
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
                if (!notification.Is_Read)
                {
                    if (notification.Message_Type == "All")
                    {
                        //await notificationHub.SendNotificationToAll(notification.Message);
                        await _hubContextNotification.Clients.All.SendAsync("ReceivedNotification", notification.Message);
                    }
                    else if (notification.Message_Type == "Personal")
                    {
                        var hubConnections = await _connectionRepository.SelectHubConnection(notification.Resident_ID);

                        if (hubConnections != null && hubConnections.Count > 0)
                        {
                            foreach (var hubConnection in hubConnections)
                            {
                                int res_id = Convert.ToInt32(notification.Resident_ID);

                                if (notification.Title.Contains("Water Reading"))
                                {
                                    await _hubContextNotification.Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedReadingNotification", notification.Title, res_id);
                                }
                                else if (notification.Title.Contains("Water Billing"))
                                {
                                    await _hubContextNotification.Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedBillingNotification", notification.Title, res_id);
                                }
                                else if (notification.Title.Contains("Register Address"))
                                {
                                    await _hubContextNotification.Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedAddressNotification", notification.Message, res_id);
                                }

                                await _hubContextNotification.Clients.Client(hubConnection.Connection_ID).SendAsync("ReceivedPersonalNotification", notification.Title, res_id);
                            }
                        }
                    }
                }

            }
        }


    }
}
