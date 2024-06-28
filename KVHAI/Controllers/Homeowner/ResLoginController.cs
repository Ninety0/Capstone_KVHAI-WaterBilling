using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Homeowner

{
    public class ResLoginController : Controller
    {
        private readonly ResidentRepository _residentRepository;
        private readonly InputSanitize _sanitize;

        public ResLoginController(ResidentRepository residentRepository, InputSanitize sanitize)
        {
            _residentRepository = residentRepository;
            _sanitize = sanitize;
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
        public async Task<IActionResult> Signup(Resident formData)
        {
            try
            {
                if (formData == null || !ModelState.IsValid)
                {
                    ViewBag.error = "Invalid Data";
                    return RedirectToAction("Signup");
                }

                int result = await _residentRepository.CreateEmployee(formData);
                if (result == 0)
                    return BadRequest(new { message = result });

                return Ok(new { message = result });
            }
            catch (Exception)
            {
                ViewBag.error = "An error occurred while processing your request.";
                return RedirectToAction("Signup");
            }
        }

    }
}
