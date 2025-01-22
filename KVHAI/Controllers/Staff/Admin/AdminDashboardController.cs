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
        private readonly WaterBillRepository _waterBillRepository;


        public AdminDashboardController(ForecastingRepo forecasting, AddressRepository addressRepository, PaymentRepository paymentRepository, RequestDetailsRepository requestDetailsRepository, NotificationRepository notification, WaterBillingFunction waterBillingFunction, WaterReadingRepository waterReadingRepository, WaterBillRepository waterBillRepository)
        {
            _forecasting = forecasting;
            _addressRepository = addressRepository;
            _paymentRepository = paymentRepository;
            _requestDetailsRepository = requestDetailsRepository;
            _notification = notification;
            _waterBillingFunction = waterBillingFunction;
            _waterReadingRepository = waterReadingRepository;
            _waterBillRepository = waterBillRepository;
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

        #region WATER BILLING
        public async Task<IActionResult> WaterBilling()
        {
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;

            var notifList = await _notification.GetNotificationByStaff(role);

            var pagination1 = new Pagination<WaterBilling>
            {
                ModelList = await _waterBillRepository.WaterBillingPagination("paid", "1"),
                ScriptName = "wbpagination"
            };
            pagination1.NumberOfData = pagination1.ModelList.Count;

            pagination1.set(10, 5, 1);

            var viewmodel = new ModelBinding
            {
                NotificationStaff = notifList,
                WaterBillingPagination = pagination1
            };
            foreach (var item in viewmodel.WaterBillingPagination.ModelList)
            {

            }
            return View("~/Views/Staff/Admin/WaterBilling.cshtml", viewmodel);
        }

        [HttpPost]
        public async Task<IActionResult> GetBilling(int page_index, string date, string status, string location, string search = "", string category = "")
        {
            try
            {
                // Parse and validate startDate and endDate
                DateTime? parsedDate = DateTime.TryParse(date, out DateTime resultD) ? resultD : (DateTime?)null;

                // Fetch total count for pagination
                var numberOfData = await _waterBillRepository.CountNumberWaterBilling(parsedDate?.ToString("yyyy-MM-dd"), status, location, search, category);

                // Initialize and configure pagination
                var pagination = new Pagination<WaterBilling>
                {
                    NumberOfData = numberOfData,
                    ScriptName = "wbpagination"
                };
                pagination.set(10, 5, page_index);

                // Fetch paginated data
                pagination.ModelList = await _waterBillRepository.WaterBillingPagination(pagination.Offset, 10, parsedDate?.ToString("yyyy-MM-dd"),
                    status, location, search, category);

                // Prepare the view model
                var viewmodel = new ModelBinding
                {
                    WaterBillingPagination = pagination
                };

                // Return the appropriate view
                return View("~/Views/Staff/Admin/WaterBilling.cshtml", viewmodel);

            }
            catch (Exception ex)
            {
                // Log the exception internally (replace with your logging framework)
                Console.Error.WriteLine($"Error in OnlinePaymentPagination: {ex.Message}");

                // Return a generic error message to the client
                return BadRequest("An error occurred while processing your request. Please try again later.");
            }
        }
        #endregion

    }
}
