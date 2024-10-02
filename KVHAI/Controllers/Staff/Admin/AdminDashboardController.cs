using KVHAI.CustomClass;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Admin
{
    public class AdminDashboardController : Controller
    {
        private readonly Forecasting _forecasting;

        public AdminDashboardController(Forecasting forecasting)
        {
            _forecasting = forecasting;
        }
        public IActionResult Index()
        {
            return View("~/Views/Staff/Admin/Dashboard.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GraphWaterConsumption()
        {
            var model = await _forecasting.GetPercentChange();
            return Json(model);
        }
    }
}
