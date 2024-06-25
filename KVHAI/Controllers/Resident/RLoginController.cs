using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Resident
{
    public class RLoginController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Resident/RLogin/Index.cshtml");
        }

        public IActionResult Signup()
        {
            return View("~/Views/Resident/Signup/Index.cshtml");
        }


    }
}
