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
        private readonly RenterRepository _renterRepository;

        public OwnerHomeController(NotificationRepository notificationRepository, StreetRepository streetRepository, AddressRepository addressRepository, ResidentAddressRepository residentAddress, RenterRepository renterRepository)
        {
            _notification = notificationRepository;
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
            _residentAddress = residentAddress;
            _renterRepository = renterRepository;
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
                ResidentAddress = residentAddress,
                ListAddress = addresses

            };

            //owner
            return View("~/Views/Resident/LoggedIn/Owner/OwnerHome.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterRenter(KVHAI.Models.Renter renter)
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (renter == null)
            {
                return BadRequest("There was en error processing the account. Please try again later");
            }

            //CHECK IF TENANT EXIST OR THE RESIDENT ID
            if (!await _renterRepository.IsTenantExist(residentID))
            {
                return BadRequest("There was en error processing the account. Please try again later");
            }

            //CHECK IF USERNAME ALREADY EXIST
            if (await _renterRepository.IsUsernameExist(renter.Username))
            {
                return BadRequest("Username was already been used.");
            }

            int insertResult = await _renterRepository.InsertRenter(residentID, renter);

            return insertResult > 0 ? Ok("Registered Successfully") : BadRequest("There was en error processing the account. Please try again later");
        }

        [HttpGet]
        public async Task<IActionResult> GetRenter(string address_id)
        {
            var requestAddress = await _residentAddress.GetRenter(address_id);
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var renterList = await _renterRepository.GetRenters(address_id);

            var viewModel = new ModelBinding
            {
                RequestAddressList = requestAddress,
                ResidentList = renterList

            };
            return View("~/Views/Resident/LoggedIn/Owner/OwnerHome.cshtml", viewModel);

        }

        //[HttpGet]
        //public async Task<IActionResult> GetRentalForOwner(string address_id)
        //{
        //    var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //    var requestAddress = await _residentAddress.GetRentalApplication(address_id);

        //    var renterList = await _renterRepository.GetRenters(residentID, address_id);


        //    var viewModel = new ModelBinding
        //    {
        //        RequestAddressList = requestAddress,
        //        RenterList = renterList


        //    };
        //    return View("~/Views/Resident/LoggedIn/Owner/OwnerHome.cshtml", viewModel);

        //}

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
