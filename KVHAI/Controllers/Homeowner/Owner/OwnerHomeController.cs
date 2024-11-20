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

            var addresses = await _addressRepository.GetAddressessByResId(residentID);
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
        public async Task<IActionResult> GetRenter(string address_id)
        {
            var requestAddress = await _residentAddress.GetRenter(address_id);

            var viewModel = new ModelBinding
            {
                RequestAddressList = requestAddress

            };
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

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string residentAddress_id, string address_id, string resident_id, string status)
        {
            var statusResult = await _residentAddress.UpdateStatus(residentAddress_id, address_id, resident_id, status);

            if (statusResult < 1)
            {
                return BadRequest("There was an error processing the request.");
            }

            return Ok();

        }
    }
}
