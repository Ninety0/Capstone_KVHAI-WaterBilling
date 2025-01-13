using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KVHAI.Controllers.Homeowner
{
    public class SettingsController : Controller
    {
        private readonly NotificationRepository _notification;
        private readonly ResidentRepository _residentRepository;

        public SettingsController(NotificationRepository notificationRepository, ResidentRepository residentRepository)
        {
            _notification = notificationRepository;
            _residentRepository = residentRepository;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var notifList = await _notification.GetNotificationByResident(residentID);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var residentList = await _residentRepository.GetResidentAccount(residentID);

            var model = new ModelBinding
            {
                NotificationResident = notifList,
                ResidentList = residentList
            };

            return View("~/Views/Resident/LoggedIn/AccountSettings.cshtml", model);
            //return View("~/Views/Resident/LoggedIn/Bills.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetResidentAccount()
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var notifList = await _notification.GetNotificationByResident(residentID);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var residentList = await _residentRepository.GetResidentAccount(residentID);

            var model = new ModelBinding
            {
                NotificationResident = notifList,
                ResidentList = residentList
            };

            return View("~/Views/Resident/LoggedIn/AccountSettings.cshtml", model);
            //return View("~/Views/Resident/LoggedIn/Bills.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateResidentAccount(Resident resident)
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var residentResult = await _residentRepository.UpdateResidentInformation(residentID, resident);

            if (residentResult < 1)
            {
                return BadRequest("There was an error updating your account.");
            }

            return Ok();
            //return View("~/Views/Resident/LoggedIn/Bills.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ValidatePassword(string password)
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var isAccountExist = await _residentRepository.IsPasswordCorrect(residentID, password);

            if (isAccountExist)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }

        }
    }
}
