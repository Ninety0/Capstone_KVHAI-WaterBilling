using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace KVHAI.Controllers.Homeowner
{
    public class ResidentHomeController : Controller
    {
        private readonly AddressRepository _addressRepository;
        private readonly NotificationRepository _notification;
        private readonly StreetRepository _streetRepository;
        private readonly ResidentAddressRepository _residentAddress;


        public ResidentHomeController(NotificationRepository notificationRepository, StreetRepository streetRepository, AddressRepository addressRepository, ResidentAddressRepository residentAddress)
        {
            _notification = notificationRepository;
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
            _residentAddress = residentAddress;
        }

        [Authorize]
        public async Task<IActionResult> RenterHome()
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var streets = await _streetRepository.GetAllStreets();
            var notifList = await _notification.GetNotificationByResident(residentID);



            var viewModel = new ModelBinding
            {
                NotificationResident = notifList,
                ListStreet = streets
            };

            return View("~/Views/Resident/LoggedIn/Renter/RenterRegisterAddress.cshtml", viewModel);
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            var res_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var notifList = await _notification.GetNotificationByResident(res_id);

            var viewModel = new ModelBinding
            {
                Resident_ID = res_id,
                Username = username,
                Role = role,
                NotificationResident = notifList,
            };
            //await _hubContext.Clients.All.SendAsync("ShowAnnouncement");
            if (role == "2")//renter
            {
                return View("~/Views/Resident/LoggedIn/Renter/RenterHome.cshtml", viewModel);
            }
            //owner
            return RedirectToAction("OwnerHome", "OwnerHome");
        }

        [HttpPost]
        public async Task<IActionResult> InsertAddress(ResidentAddress address)
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var st_id = await _streetRepository.GetStreetID(address);

            address = new ResidentAddress
            {
                Block = address.Block,
                Lot = address.Lot,
                ID = st_id
            };

            int address_id = await _addressRepository.GetAddressIDForRenter(address);

            if (address_id < 1)
            {
                return BadRequest("There was an error registering the address.");
            }

            int insertResult = await _addressRepository.InsertRenterAddress(residentID, address_id.ToString());

            if (insertResult < 1)
            {
                return BadRequest("There was an error registering the address.");
            }

            return Ok("Address added successfully. Please wait for tenant approve your application.");
        }

        [HttpGet]
        public async Task<IActionResult> GetApplication(string resident_address_tb)
        {
            var res_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var requestAddress = await _residentAddress.GetRentalApplicationForRenter(res_id);

            var viewModel = new ModelBinding
            {
                RequestAddressList = requestAddress

            };
            //await _hubContext.Clients.All.SendAsync("ShowAnnouncement");
            if (role == "2")//renter
            {
                return View("~/Views/Resident/LoggedIn/Renter/RenterHome.cshtml", viewModel);
            }
            else
            {
                return BadRequest("There was an error fetching the application.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelRequest(string resident_address)
        {
            var cancelResult = await _residentAddress.CancelRequest(resident_address);
            var delResult = await _residentAddress.DeleteRenter(resident_address);

            if (delResult < 1)
            {
                return BadRequest("There was an error processing your request. Please try again later.");
            }

            return Ok("Deleted Successfully.");

        }
    }
}
