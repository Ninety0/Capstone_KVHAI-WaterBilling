using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Homeowner

{
    public class ResLoginController : Controller
    {
        private readonly ResidentRepository _residentRepository;
        private readonly ImageUploadRepository _imageRepository;
        private readonly InputSanitize _sanitize;
        private readonly IWebHostEnvironment _environment;

        public ResLoginController(ResidentRepository residentRepository, InputSanitize sanitize, ImageUploadRepository imageUpload, IWebHostEnvironment environment)
        {
            _residentRepository = residentRepository;
            _sanitize = sanitize;
            _imageRepository = imageUpload;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View("~/Views/Resident/RLogin/Index.cshtml");
        }

        public IActionResult Signup()
        {
            return View("~/Views/Resident/Signup/Index.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(Resident formData, IFormFile file)
        {
            try
            {
                if (formData == null || !ModelState.IsValid)
                {
                    return BadRequest(new { message = 0 });
                }

                if (await _residentRepository.UserExists(formData))
                {
                    return Ok(new { message = "exist" });
                }

                int result = await _residentRepository.CreateResidentandUploadImage(formData, file, _environment.WebRootPath);

                if (result == 0)
                    return BadRequest(new { message = "There was an error saving the resident and the image." });

                return Ok(new { message = "Registration Successful." });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "An error occurred while processing your request." });
            }
        }

    }
}
