using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Admin
{
    public class AdminDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Staff/Admin/Dashboard.cshtml");
        }
    }
}
