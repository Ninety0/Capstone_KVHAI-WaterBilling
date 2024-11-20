using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KVHAI.Controllers.Homeowner
{
    public class MyAddressController : Controller
    {
        private readonly StreetRepository _streetRepository;
        private readonly AddressRepository _addressRepository;
        private readonly NotificationRepository _notification;
        private readonly ResidentAddressRepository _residentAddressRepository;
        private readonly ListRepository _listRepository;


        public MyAddressController(StreetRepository streetRepository, AddressRepository addressRepository, NotificationRepository notification, ResidentAddressRepository residentAddressRepository, ListRepository listRepository)
        {
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
            _notification = notification;
            _residentAddressRepository = residentAddressRepository;
            _listRepository = listRepository;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var address = new List<Models.Address>();
            var listStreet = await _streetRepository.GetAllStreets();
            var notifList = await _notification.GetNotificationByResident(residentID);


            if (role == "1")
            {
                address = await _addressRepository.GetAddressessByResId(residentID);
            }
            else
            {
                address = await _residentAddressRepository.GetAddressessByResId(residentID);
            }

            var model = new ModelBinding
            {
                ListStreet = listStreet,
                ListAddress = address,
                NotificationResident = notifList
            };
            return View("~/Views/Resident/LoggedIn/MyAddress.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAddress(string resident_id)
        {
            var address = await _addressRepository.GetAddressessByResId(resident_id);
            var listStreet = await _streetRepository.GetAllStreets();

            //var listStreet = await _streetRepository.GetAllStreets();
            //var address = await _addressRepository.GetAddressById(resident_id);
            var model = new ModelBinding
            {
                ListStreet = listStreet,
                ListAddress = address,
            };

            return View("~/Views/Resident/LoggedIn/MyAddress.cshtml", model); //model);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveToken(string addressID)
        {
            var resID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tokenExist = await _addressRepository.CheckRemoveTokenExist(addressID, resID);
            int result = await _addressRepository.RequestRemoveTokenUpdate(addressID, resID);

            if (tokenExist)
            {
                return BadRequest("There was an existing request made.");
            }

            if (result < 1)
            {
                return BadRequest("There was an error requesting for address removal. Please Try again later.");
            }

            var emp = await _listRepository.EmployeeList();
            var emp_id = emp.Where(r => r.Role == "admin").Select(e => e.Emp_ID).ToList();
            var notif = new Notification
            {
                Title = "Request Action",
                Message = "Request of removing address",
                Url = "/kvhai/staff/request-page/",
                Message_Type = "admin",
                ListEmployee_ID = emp_id
            };

            await _notification.SendNotificationToAdmin(notif);

            return Ok(new { message = "Request of removing address are now on process.", request_id = result });
        }

        [HttpPost]
        public async Task<IActionResult> CancelRemoveToken(string addressID, string requestID)
        {
            var resID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tokenExist = await _addressRepository.CheckRemoveTokenExist(addressID, resID);
            int result = await _addressRepository.CancelRequestRemoveTokenUpdate(addressID, resID, requestID);

            if (!tokenExist)
            {
                return BadRequest("There was no existing request made.");
            }

            if (result < 1)
            {
                return BadRequest("There was an error canceling the request.");
            }

            return Ok("Your request of removing address are now canceled.");
        }

    }
}

/*
 
 UPDATE NEEDED ARE:
    - BUTTONS FOR DELETE AND CANCEL DID NOT CHANGE
    - WHEN REQUEST MADE WHEN PAGE REFRESH BUTTONS SHOULD BE UPDATED TO
    - DISPLAY OF REQUEST ON ADMIN
 
 */