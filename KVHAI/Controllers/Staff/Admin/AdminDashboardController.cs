using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KVHAI.Controllers.Staff.Admin
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth", Roles = "admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ForecastingRepo _forecasting;
        private readonly AddressRepository _addressRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly RequestDetailsRepository _requestDetailsRepository;
        private readonly NotificationRepository _notification;



        public AdminDashboardController(ForecastingRepo forecasting, AddressRepository addressRepository, PaymentRepository paymentRepository, RequestDetailsRepository requestDetailsRepository, NotificationRepository notification)
        {
            _forecasting = forecasting;
            _addressRepository = addressRepository;
            _paymentRepository = paymentRepository;
            _requestDetailsRepository = requestDetailsRepository;
            _notification = notification;
        }

        public async Task<IActionResult> Index()
        {
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;

            var notifList = await _notification.GetNotificationByStaff(role);

            var viewmodel = new ModelBinding
            {
                NotificationStaff = notifList
            };

            return View("~/Views/Staff/Admin/Dashboard.cshtml", viewmodel);
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
            int count = model.Count;
            var modelBinding = new ModelBinding
            {
                ResidentAddress = model,
                CountData = count

            };

            //return Json(model);
            return View("~/Views/Staff/Admin/Dashboard.cshtml", modelBinding);
        }

        [HttpGet]
        public async Task<IActionResult> GetRequest()
        {
            var model = await _requestDetailsRepository.GetPendingRemovalRequests();
            int count = model.Count;
            var modelBinding = new ModelBinding
            {
                CountData = count

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
