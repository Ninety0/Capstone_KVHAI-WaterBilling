using Microsoft.AspNetCore.SignalR;

namespace KVHAI.Hubs
{
    public class WaterReadingHub : Hub
    {
        public async Task UpdateReading()
        {
            await Clients.All.SendAsync("ReceiveReading");
        }

        public async Task UpdateWaterBillNo()
        {
            await Clients.All.SendAsync("ReceiveWaterBillNo");
        }
    }
}
