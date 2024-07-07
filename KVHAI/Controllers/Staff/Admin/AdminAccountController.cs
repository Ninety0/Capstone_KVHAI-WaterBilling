using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Admin
{
    public class AdminAccountController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Staff/Admin/Account.cshtml");
        }
    }
}
