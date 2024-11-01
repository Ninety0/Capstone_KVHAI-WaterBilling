using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Admin
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth", Roles = "admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ForecastingRepo _forecasting;
        private readonly AddressRepository _addressRepository;
        private readonly PaymentRepository _paymentRepository;

        public AdminDashboardController(ForecastingRepo forecasting, AddressRepository addressRepository, PaymentRepository paymentRepository)
        {
            _forecasting = forecasting;
            _addressRepository = addressRepository;
            _paymentRepository = paymentRepository;
        }

        public IActionResult Index()
        {
            var modelBinding = new ModelBinding();
            return View("~/Views/Staff/Admin/Dashboard.cshtml", modelBinding);
        }

        [HttpGet]
        public async Task<IActionResult> GraphWaterConsumption()
        {
            var model = await _forecasting.GetPercentChange();
            return Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetRegisterAddress()
        {
            var model = await _addressRepository.GetNewRegisteringAddress();
            var modelBinding = new ModelBinding
            {
                ResidentAddress = model
            };

            //return Json(model);
            return View("~/Views/Staff/Admin/Dashboard.cshtml", modelBinding);
        }

        [HttpGet]
        public async Task<IActionResult> GetPayments()
        {
            var payment = await _paymentRepository.GetNewPayment();
            var modelBinding = new ModelBinding
            {
                PaymentList = payment,
            };
            if (payment == null)
            {
                return BadRequest("Null");
            }
            //return Json(model);
            return View("~/Views/Staff/Admin/Dashboard.cshtml", modelBinding);
        }
    }
}
