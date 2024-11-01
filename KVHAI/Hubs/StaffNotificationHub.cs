using Microsoft.AspNetCore.SignalR;

namespace KVHAI.Hubs
{
    public class StaffNotificationHub : Hub
    {
        public async Task NotifyWaterReading()
        {
            await Clients.All.SendAsync("ReceivedWaterReading");
        }

        public async Task NotifyWaterBilling()
        {
            await Clients.All.SendAsync("ReceivedWaterBilling");
        }
    }
}
