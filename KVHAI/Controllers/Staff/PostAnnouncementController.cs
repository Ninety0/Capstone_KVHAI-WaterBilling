using KVHAI.CustomClass;
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
        private readonly Pagination<Announcement> _pagination;



        public PostAnnouncementController(AnnouncementRepository announcementRepository, DBConnect dBConnect, IHubContext<AnnouncementHub> hubContext, NotificationRepository notification, Pagination<Announcement> pagination)
        {
            _announcementRepository = announcementRepository;
            _dBConnect = dBConnect;
            _hubContext = hubContext;
            _notification = notification;
            _pagination = pagination;
        }
        public async Task<IActionResult> Index()
        {
            var pagination = new Pagination<Announcement>();
            return View("~/Views/Staff/PostAnnouncement/PostAnnouncement.cshtml", pagination);
        }

        [HttpGet]
        public async Task<IActionResult> GetAnnouncement(int page_index = 1)
        {

            var pagination = new Pagination<Announcement>
            {
                ModelList = await _announcementRepository.GetAllAnnouncement(offset: 0, limit: 10),
                NumberOfData = await _announcementRepository.CountAnnouncementData(),
                ScriptName = "announce_pagination"
            };
            pagination.set(10, 5, page_index);
            pagination.ModelList = await _announcementRepository.GetAllAnnouncement(pagination.Offset, 10);
            return View("~/Views/Staff/PostAnnouncement/PostAnnouncement.cshtml", pagination);
        }

        [HttpGet]
        public async Task<IActionResult> GetSingleData(int id)
        {
            var announcements = await _announcementRepository.GetAnnouncements(id.ToString());

            if (announcements == null || !announcements.Any())
            {
                return NotFound("No announcement found with the specified ID.");
            }

            var announce = announcements.FirstOrDefault();

            return Ok(new {announce});
        }

        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Save(Announcement announce, List<IFormFile>? images = null)
        {
            if (images?.Any() == true)
            {
                var result = await _announcementRepository.Save(announce, images);

                if (result < 1)
                {
                    return BadRequest("There was an error posting the announcement. Please try again later.");
                }

            }
            else
            {
                // var result = await _announcementRepository.SubmitAnnouncementNoImage(announce);
                var result = await _announcementRepository.Save(announce);


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

        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Update(Announcement announce, List<IFormFile>? images = null)
        {
            if (images?.Any() == true)
            {
                var result = await _announcementRepository.UpdateAnnouncement(announce, images);

                if (result < 1)
                {
                    return BadRequest("There was an error updating the announcement. Please try again later.");
                }

            }
            else
            {
                // var result = await _announcementRepository.SubmitAnnouncementNoImage(announce);
                var result = await _announcementRepository.UpdateAnnouncement(announce);


                if (result < 1)
                {
                    return BadRequest("There was an error updating the announcement. Please try again later.");
                }
            }

            return Ok("The announcement was updated successfully.");
        }


    }
}
