using System.Security.Claims;
using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Clerk
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth", Roles = "clerk")]
    public class ClerkController : Controller
    {
        private readonly WaterReadingRepository _waterReadingRepository;
        private readonly WaterBillingFunction _waterBilling;
        private readonly WaterBillRepository _waterBillRepository;
        private readonly NotificationRepository _notificationRepository;

        public ClerkController(WaterBillingFunction waterBilling, WaterBillRepository waterBillRepository, NotificationRepository notificationRepository)//WaterReadingRepository waterReadingRepository
        {
            _waterBilling = waterBilling;
            _waterBillRepository = waterBillRepository;
            _notificationRepository = notificationRepository;
        }

        public async Task<IActionResult> Index()
        {
            //var prevReading = await _waterReadingRepository.GetPreviousReading();
            //var currentReading = await _waterReadingRepository.GetCurrentReading();

            //var model = new ModelBinding
            //{
            //    PreviousReading = prevReading.PreviousReading,
            //    CurrentReading = currentReading.CurrentReading,
            //    ResidentAddress = prevReading.ResidentAddress

            //};

            //await _waterBilling.WaterReadingFunction("1");
            var username = User.Identity.Name;
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var notifs = await _notificationRepository.GetNotificationByStaff(role);
            _waterBilling.NotificationStaff = notifs;
            await _waterBilling.WaterReading(location: "1");

            return View("~/Views/Staff/Clerk/Index.cshtml", _waterBilling);
        }

        public async Task<IActionResult> Dashboard()
        {
            return View("~/Views/Staff/Clerk/ClerkDashboard.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetNotification()
        {
            var username = User.Identity.Name;
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var notifs = await _notificationRepository.GetNotificationByStaff(role);
            _waterBilling.NotificationStaff = notifs;
            
            return View("~/Views/Staff/Clerk/Index.cshtml", _waterBilling);
        }

        [HttpGet]
        public async Task<IActionResult> WaterReadLocation(string location = "", string fromDate = "", string toDate = "")
        {
            try
            {
                if (DateTime.TryParse(fromDate, out DateTime _from))
                {
                    fromDate = _from.ToString("yyyy-MM");
                }
                if (DateTime.TryParse(toDate, out DateTime _to))
                {
                    toDate = _to.ToString("yyyy-MM");
                }
                await _waterBilling.WaterReading(location, fromDate, toDate);

                return View("~/Views/Staff/Clerk/Index.cshtml", _waterBilling);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> WaterReadingByMonth(string fromDate, string toDate, string location = "")
        {
            try
            {
                if (DateTime.TryParse(fromDate, out DateTime _from))
                {
                    fromDate = _from.ToString("yyyy-MM");
                }
                if (DateTime.TryParse(toDate, out DateTime _to))
                {
                    toDate = _to.ToString("yyyy-MM");
                }
                await _waterBilling.WaterReading(location, fromDate, toDate);

                return View("~/Views/Staff/Clerk/Index.cshtml", _waterBilling);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBilling(List<WaterBilling> waterBilling)//([FromBody] ModelBinding binding)//int id, string amount, string date)
        {
            try
            {
                if (waterBilling == null || waterBilling.Count < 1)
                {
                    return BadRequest("There was an error generating the bill");
                }

                var ListBilling = new List<WaterBilling>();
                var billing = await _waterBillRepository.GetDateBilling();//DATE ISSUE OF BILL
                var due = await _waterBillRepository.GetDueDate();                    //DUE DATE OF BILL
                var status = "unpaid";

                foreach (var item in waterBilling)
                {
                    //if (item == null)
                    //{
                    //    return BadRequest("There was an error processing the data.");
                    //}

                    var wb = new WaterBilling()
                    {
                        Address_ID = item.Address_ID,
                        Cubic_Meter = item.Cubic_Meter,
                        Amount = item.Amount,
                        Date_Issue_From = billing.Date_Issue_From,
                        Date_Issue_To = billing.Date_Issue_To,
                        Due_Date_From = due.Due_Date_From,
                        Due_Date_To = due.Due_Date_To,
                        Status = status
                    };

                    if (string.IsNullOrEmpty(wb.Date_Issue_From) || string.IsNullOrEmpty(wb.Date_Issue_To) || string.IsNullOrEmpty(wb.Due_Date_From) || string.IsNullOrEmpty(wb.Due_Date_To))
                    {
                        return BadRequest("There was an error processing the data.");
                    }

                    ListBilling.Add(wb);

                }

                if (await _waterBillRepository.CheckExistingWaterBilling(ListBilling))
                {
                    return BadRequest("The reading in current location is already done.");
                }

                int result = await _waterBillRepository.CreateWaterBill(ListBilling);
                if (result < 1)
                {
                    return BadRequest("There was an error processing the data.");
                }


                return Ok("Generate bill successfully. Proceed to Billing.");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateWaterBilling(List<WaterBilling> waterBilling)//([FromBody] ModelBinding binding)//int id, string amount, string date)
        {
            try
            {
                if (waterBilling == null || waterBilling.Count < 1)
                {
                    return BadRequest("There was an error generating the bill");
                }

                var ListBilling = new List<WaterBilling>();
                var billing = await _waterBillRepository.GetDateBilling();//DATE ISSUE OF BILL
                var due = await _waterBillRepository.GetDueDate();                    //DUE DATE OF BILL
                var status = "unpaid";

                foreach (var item in waterBilling)
                {
                    //if (item == null)
                    //{
                    //    return BadRequest("There was an error processing the data.");
                    //}

                    var wb = new WaterBilling()
                    {
                        Address_ID = item.Address_ID,
                        Cubic_Meter = item.Cubic_Meter,
                        Previous_Reading = item.Previous_Reading,
                        Current_Reading = item.Current_Reading,
                        Amount = item.Amount,
                        Bill_For = item.Bill_For,
                        Date_Issue_From = billing.Date_Issue_From,
                        Date_Issue_To = billing.Date_Issue_To,
                        Due_Date_From = due.Due_Date_From,
                        Due_Date_To = due.Due_Date_To,
                        Status = status
                    };

                    if (string.IsNullOrEmpty(wb.Date_Issue_From) || string.IsNullOrEmpty(wb.Date_Issue_To) || string.IsNullOrEmpty(wb.Due_Date_From) || string.IsNullOrEmpty(wb.Due_Date_To))
                    {
                        return BadRequest("There was an error processing the data.");
                    }

                    ListBilling.Add(wb);

                }

                if (await _waterBillRepository.CheckExistingWaterBilling(ListBilling))
                {
                    return BadRequest("The reading in current location is already done.");
                }

                int result = await _waterBillRepository.CreateWaterBill(ListBilling);
                if (result < 1)
                {
                    return BadRequest("There was an error processing the data.");
                }


                return Ok("Generate bill successfully. Proceed to Billing.");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
