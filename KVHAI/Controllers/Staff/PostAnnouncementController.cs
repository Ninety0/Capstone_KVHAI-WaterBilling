using KVHAI.Hubs;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace KVHAI.Controllers.Staff
{
    public class PostAnnouncementController : Controller
    {
        private readonly DBConnect _dBConnect;
        private readonly AnnouncementRepository _announcementRepository;
        private readonly IHubContext<AnnouncementHub> _hubContext;
        private readonly NotificationRepository _notification;


        public PostAnnouncementController(AnnouncementRepository announcementRepository, DBConnect dBConnect, IHubContext<AnnouncementHub> hubContext, NotificationRepository notification)
        {
            _announcementRepository = announcementRepository;
            _dBConnect = dBConnect;
            _hubContext = hubContext;
            _notification = notification;
        }
        public async Task<IActionResult> Index()
        {
            return View("~/Views/Staff/PostAnnouncement/PostAnnouncement.cshtml");
        }

        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Save(Announcement announce, List<IFormFile> images)
        {
            if (images != null && images.Count > 0)
            {
                var result = await _announcementRepository.Save(announce, images);

                if (result < 1)
                {
                    return BadRequest("There was an error posting the announcement. Please try again later.");
                }

            }
            else
            {
                var result = await _announcementRepository.SubmitAnnouncementNoImage(announce);

                if (result < 1)
                {
                    return BadRequest("There was an error posting the announcement. Please try again later.");
                }
            }

            var notif = new Notification
            {
                Title = "Announcement",
                Message = "New announcement was posted!",
                Url = "/kvhai/resident/home/",
                Message_Type = "All"
            };

            await _notification.InsertNotificationPersonal(notif);

            var model = await _announcementRepository.ShowAnnouncement();

            await _hubContext.Clients.All.SendAsync("ShowAnnouncement");

            return Ok("The announcement was posted successfully.");
        }

    }
}
