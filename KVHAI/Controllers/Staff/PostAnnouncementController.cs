using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff
{
    public class PostAnnouncementController : Controller
    {
        private readonly DBConnect _dBConnect;
        private readonly AnnouncementRepository _announcementRepository;

        public PostAnnouncementController(AnnouncementRepository announcementRepository, DBConnect dBConnect)
        {
            _announcementRepository = announcementRepository;
            _dBConnect = dBConnect;
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

            return Ok("The announcement was posted successfully.");
        }

    }
}
