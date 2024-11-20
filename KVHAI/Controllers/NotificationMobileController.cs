using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KVHAI.Controllers
{
    public class NotificationMobileController : Controller
    {
        private readonly NotificationRepository _notification;

        public NotificationMobileController(NotificationRepository notification)
        {
            _notification = notification;
        }

        public async Task<IActionResult> Index()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var notifList = await _notification.GetNotificationByResident(residentID);
            int addressID = 0;

            var viewModel = new ModelBinding
            {
                NotificationResident = notifList,
            };
            return View("~/Views/Resident/LoggedIn/NotificationMobile.cshtml", viewModel);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetNewNotification(string resident_id)
        //{
        //    var role = User.FindFirst(ClaimTypes.Role)?.Value;
        //    var model = await _announcementRepository.ShowAnnouncement();

        //    var address_id = await _addressRepository.GetAddressIDByResidentID(resident_id, role);

        //    var announcments = await _announcementRepository.ShowAnnouncement();
        //    var notifList = await _notification.GetNotificationByResident(resident_id);

        //    var viewModel = new ModelBinding
        //    {
        //        NotificationResident = notifList,
        //        AnnouncementList = model
        //    };

        //    return View("~/Views/Resident/LoggedIn/Announcement.cshtml", viewModel);
        //}
    }
}
