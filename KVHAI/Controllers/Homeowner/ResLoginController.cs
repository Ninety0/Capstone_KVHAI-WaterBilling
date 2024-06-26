using KVHAI.CustomClass;
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
        public IActionResult Signup(string data)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.error = "Invalid Credentials";

                return RedirectToAction("Signup");
            }

            return RedirectToAction("Signup");
        }
    }
}
