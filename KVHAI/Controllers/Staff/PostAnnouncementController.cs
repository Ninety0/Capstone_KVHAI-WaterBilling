using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff
{
    public class PostAnnouncementController : Controller
    {
        private readonly AnnouncementRepository _announcementRepository;

        public PostAnnouncementController(AnnouncementRepository announcementRepository)
        {
            _announcementRepository = announcementRepository;
        }
        public async Task<IActionResult> Index()
        {
            return View("~/Views/Staff/PostAnnouncement/PostAnnouncement.cshtml");
        }

        [AutoValidateAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> Save(Announcement announce)
        {
            var result = await _announcementRepository.SubmitAnnouncement(announce);

            if (result < 1)
            {
                return BadRequest("There was an error posting the announcement. Please try again later.");
            }

            return Ok("The announcement was posted");
        }
    }
}
