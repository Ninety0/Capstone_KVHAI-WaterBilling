using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Admin
{
    public class AdminAccountController : Controller
    {
        private readonly ResidentRepository _residentRepository;
        private readonly IWebHostEnvironment _environment;

        public AdminAccountController(ResidentRepository residentRepository, IWebHostEnvironment environment)
        {
            _residentRepository = residentRepository;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var residents = await _residentRepository.GetAllResidentAsync();
            return View("~/Views/Staff/Admin/Account.cshtml", residents);
        }

        [HttpGet]
        public async Task<IActionResult> GetImageBase64(string id)
        {
            var imagePath = await _residentRepository.GetImagePathAsync(id);
            if (string.IsNullOrEmpty(imagePath))
                return Json(new { success = false });

            var fullPath = Path.Combine(_environment.WebRootPath, imagePath);
            if (!System.IO.File.Exists(fullPath))
                return Json(new { success = false });

            byte[] imageArray = await System.IO.File.ReadAllBytesAsync(fullPath);
            string base64ImageRepresentation = Convert.ToBase64String(imageArray);

            return Json(new { success = true, imageBase64 = base64ImageRepresentation });
        }
    }
}
