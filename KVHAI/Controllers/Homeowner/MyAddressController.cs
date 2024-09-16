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

        public MyAddressController(StreetRepository streetRepository, AddressRepository addressRepository)
        {
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var listStreet = await _streetRepository.GetAllStreets();
            var address = await _addressRepository.GetAddressById(residentID);

            var model = new ModelBinding
            {
                ListStreet = listStreet,
                ListAddress = address
            };
            return View("~/Views/Resident/LoggedIn/Owner/MyAddress.cshtml", model);
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