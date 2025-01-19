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
        private readonly WaterBillingFunction _waterBillingFunction;
        private readonly WaterReadingRepository _waterReadingRepository;



        public AdminDashboardController(ForecastingRepo forecasting, AddressRepository addressRepository, PaymentRepository paymentRepository, RequestDetailsRepository requestDetailsRepository, NotificationRepository notification, WaterBillingFunction waterBillingFunction, WaterReadingRepository waterReadingRepository)
        {
            _forecasting = forecasting;
            _addressRepository = addressRepository;
            _paymentRepository = paymentRepository;
            _requestDetailsRepository = requestDetailsRepository;
            _notification = notification;
            _waterBillingFunction = waterBillingFunction;
            _waterReadingRepository = waterReadingRepository;
        }

        public async Task<IActionResult> Index()
        {
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;

            var yearList = await _waterReadingRepository.GetYearList();
            var notifList = await _notification.GetNotificationByStaff(role);

            var viewmodel = new ModelBinding
            {
                NotificationStaff = notifList,
                YearList = yearList
            };

            return View("~/Views/Staff/Admin/Dashboard.cshtml", viewmodel);
        }



        [HttpGet]
        public async Task<IActionResult> GraphWaterConsumption(string year)
        {
            var forecastData = await _waterBillingFunction.GetGraphDataDatabaseAdmin(year);
            var model = await _forecasting.GetPercentChange();
            return Json(forecastData);
        }

        [HttpGet]
        public async Task<IActionResult> GetNewReading()
        {
            var _model = await _waterReadingRepository.GetLatestReadingByMonth();
            var model = await _addressRepository.GetNewRegisteringAddress();
            //int count = model.Count;
            //var modelBinding = new ModelBinding
            //{
            //    ResidentAddress = model,
            //    CountData = count

            //};
            //return Json(model);
            return View("~/Views/Staff/Admin/Dashboard.cshtml", _model);
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

        [HttpGet]
        public async Task<IActionResult> GetRemit(string date)
        {
            var payment = await _paymentRepository.GetNewPayment(date);
            double totalAmount = 0;

            if (payment == null)
            {
                return BadRequest("Null");
            }

            foreach (var item in payment)
            {
                totalAmount += Convert.ToDouble(item.Paid_Amount);
            }

            return Json(totalAmount.ToString("F2"));

        }

        #region WaterREADING
        public async Task<IActionResult> WaterReading()
        {
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;

            var notifList = await _notification.GetNotificationByStaff(role);

            var viewmodel = new ModelBinding
            {
                NotificationStaff = notifList,
            };

            return View("~/Views/Staff/Admin/WaterReading.cshtml", viewmodel);
        }

        [HttpGet]
        public async Task<IActionResult> GetConsumptionByMonth(string status, string location)
        {
            var viewmodel = await _waterReadingRepository.GetWaterConsumptionByMonth(status, location);

            if (viewmodel == null)
            {
                return BadRequest("There was an error fetching the data.");
            }

            return View("~/Views/Staff/Admin/WaterReading.cshtml", viewmodel);

        }
        #endregion

    }
}
