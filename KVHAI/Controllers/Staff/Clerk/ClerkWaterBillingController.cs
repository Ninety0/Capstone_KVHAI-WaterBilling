using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Clerk
{
    public class ClerkWaterBillingController : Controller
    {
        public IActionResult Index()
        {
            //return View("ClerkWaterBilling");
            return View("~/Views/Staff/Clerk/CWaterBilling.cshtml");
            //return View("~/Views/Staff/Clerk/Index.cshtml", _waterBilling);
        }
    }
}
