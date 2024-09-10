using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers
{
    public class PageNotFoundController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
