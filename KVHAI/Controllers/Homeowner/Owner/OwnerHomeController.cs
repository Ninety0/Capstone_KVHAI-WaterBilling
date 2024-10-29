using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KVHAI.Controllers.Homeowner.Owner
{
    public class OwnerHomeController : Controller
    {
        private readonly AddressRepository _addressRepository;
        private readonly NotificationRepository _notification;
        private readonly StreetRepository _streetRepository;
        private readonly ResidentAddressRepository _residentAddress;

        public OwnerHomeController(NotificationRepository notificationRepository, StreetRepository streetRepository, AddressRepository addressRepository, ResidentAddressRepository residentAddress)
        {
            _notification = notificationRepository;
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
            _residentAddress = residentAddress;
        }

        [Authorize]
        public async Task<IActionResult> OwnerHome()
        {
            var username = User.Identity.Name;
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var addresses = await _addressRepository.GetAddressById(residentID);
            var residentAddress = await _streetRepository.GetStreetNameList(addresses);


            var notifList = await _notification.GetNotificationByResident(residentID);

            var viewModel = new ModelBinding
            {
                Resident_ID = residentID,
                Username = username,
                Role = role,
                NotificationResident = notifList,
                ResidentAddress = residentAddress

            };

            //owner
            return View("~/Views/Resident/LoggedIn/Owner/OwnerHome.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetRentalForOwner(string address_id)
        {
            var requestAddress = await _residentAddress.GetRentalApplication(address_id);

            var viewModel = new ModelBinding
            {
                RequestAddressList = requestAddress

            };
            return View("~/Views/Resident/LoggedIn/Owner/OwnerHome.cshtml", viewModel);

        }
    }
}
