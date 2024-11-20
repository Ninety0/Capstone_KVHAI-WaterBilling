using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KVHAI.Controllers.Homeowner.Renter
{
    public class RenterAddressController : Controller
    {
        private readonly StreetRepository _streetRepository;
        private readonly AddressRepository _addressRepository;
        private readonly NotificationRepository _notification;
        private readonly ResidentAddressRepository _residentAddressRepository;


        public RenterAddressController(StreetRepository streetRepository, AddressRepository addressRepository, NotificationRepository notification, ResidentAddressRepository residentAddressRepository)
        {
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
            _notification = notification;
            _residentAddressRepository = residentAddressRepository;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var listStreet = await _streetRepository.GetAllStreets();
            var address = await _residentAddressRepository.GetAddressessByResId(residentID);
            var notifList = await _notification.GetNotificationByResident(residentID);

            var model = new ModelBinding
            {
                ListStreet = listStreet,
                ListAddress = address,
                NotificationResident = notifList
            };
            return View("~/Views/Resident/LoggedIn/Renter/RenterAddress.cshtml", model);

        }
    }
}
