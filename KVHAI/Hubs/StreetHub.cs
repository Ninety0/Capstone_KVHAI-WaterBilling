using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.SignalR;

namespace KVHAI.Hubs
{
    public class StreetHub : Hub
    {
        private readonly StreetRepository _streetRepository;
        private readonly Pagination<Streets> _pagination;


        public StreetHub(StreetRepository streetRepository, Pagination<Streets> pagination)
        {
            _streetRepository = streetRepository;
            _pagination = pagination;
        }

        public async Task GetStreets()
        {
            var pagination = new Pagination<Streets>
            {
                ModelList = await _streetRepository.GetAllStreets(offset: 0, limit: 10),
                NumberOfData = await _streetRepository.CountStreetsData(),
                ScriptName = "stpagination"
            };
            pagination.set(10, 5, 1);

            await Clients.All.SendAsync("GetAllStreets", pagination);
        }

        public async Task NotifyStreetAdded(Pagination<Streets> formData)
        {
            await Clients.All.SendAsync("ReceiveStreetAdded", formData);
        }

        // Notify clients that a street has been updated
        public async Task NotifyStreetUpdated(string streetName)
        {
            await Clients.All.SendAsync("ReceiveStreetUpdated", streetName);
        }

        // Notify clients that a street has been deleted
        public async Task NotifyStreetDeleted(int streetId)
        {
            await Clients.All.SendAsync("ReceiveStreetDeleted", streetId);
        }
    }
}
