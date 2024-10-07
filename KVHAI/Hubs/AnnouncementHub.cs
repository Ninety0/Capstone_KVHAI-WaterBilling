using KVHAI.Repository;
using Microsoft.AspNetCore.SignalR;

namespace KVHAI.Hubs
{
    public class AnnouncementHub : Hub
    {
        private readonly AnnouncementRepository _announcementRepository;

        public AnnouncementHub(AnnouncementRepository announcementRepository)
        {
            _announcementRepository = announcementRepository;
        }

        // Notify clients that a street has been deleted
        public async Task NotifyAnnouncement()
        {
            await Clients.All.SendAsync("ShowAnnouncement");
        }

    }
}
