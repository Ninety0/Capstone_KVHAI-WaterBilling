using KVHAI.Hubs;
using KVHAI.Models;
using Microsoft.AspNetCore.SignalR;
using TableDependency.SqlClient;

namespace KVHAI.SubscribeSqlDependency
{
    public class SubscribeAnnouncementTableDependency : ISubscribeTableDependency
    {
        SqlTableDependency<Announcement> tableDependency;
        AnnouncementHub announcementHub;
        private readonly IHubContext<AnnouncementHub> _hubContext;

        public SubscribeAnnouncementTableDependency(AnnouncementHub announcementHub, IHubContext<AnnouncementHub> hubContext)
        {
            this.announcementHub = announcementHub;
            _hubContext = hubContext;
        }

        public void SubscribeTableDependency(string connectionString)
        {
            tableDependency = new SqlTableDependency<Announcement>(connectionString, "announcement_tb");
            tableDependency.OnChanged += TableDependency_OnChanged;
            tableDependency.OnError += TableDependency_OnError;
            tableDependency.Start();
        }

        private void TableDependency_OnError(object sender, TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs e)
        {
            Console.WriteLine($"{nameof(Announcement)} SqlTableDependency error: {e.Error.Message}");
        }

        private async void TableDependency_OnChanged(object sender, TableDependency.SqlClient.Base.EventArgs.RecordChangedEventArgs<Announcement> e)
        {
            if (e.ChangeType != TableDependency.SqlClient.Base.Enums.ChangeType.None)
            {
                await _hubContext.Clients.All.SendAsync("ShowAnnouncement");
                //announcementHub.NotifyAnnouncement();
            }
        }
    }
}
