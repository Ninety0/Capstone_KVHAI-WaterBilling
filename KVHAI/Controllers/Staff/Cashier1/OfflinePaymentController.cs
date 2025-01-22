using AspNetCore.Reporting;
using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;

namespace KVHAI.Controllers.Staff.Cashier1
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth", Roles = "cashier1")]

    public class OfflinePaymentController : Controller
    {
        private readonly StreetRepository _streetRepository;
        private readonly ResidentAddressRepository _residentAddress;
        private readonly PaymentRepository _paymentRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly NotificationRepository _notification;


        public OfflinePaymentController(StreetRepository streetRepository, ResidentAddressRepository residentAddress, PaymentRepository paymentRepository, IWebHostEnvironment webHostEnvironment, NotificationRepository notification)
        {
            _streetRepository = streetRepository;
            _residentAddress = residentAddress;
            _paymentRepository = paymentRepository;
            _webHostEnvironment = webHostEnvironment;
            _notification = notification;
        }
        public async Task<IActionResult> Index()
        {
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;

            var notifList = await _notification.GetNotificationByStaff(role);

            var streets = await _streetRepository.GetAllStreets();

            var model = new ModelBinding()
            {
                NotificationStaff = notifList,
                ListStreet = streets
            };

            return View("~/Views/Staff/Cashier1/OfflinePayment.cshtml", model);
        }

        public async Task<IActionResult> History()
        {
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;

            var notifList = await _notification.GetNotificationByStaff(role);

            var billNoList = await _paymentRepository.GetWaterBillingNumber();

            var pagination1 = new Pagination<Payment>
            {
                ModelList = await _paymentRepository.GetRecentOnlinePayment(0, 10, "offline"),
                NumberOfData = await _paymentRepository.GetCountOnlinePayment("offline"),
                ScriptName = "onpagination"
            };
            pagination1.set(10, 5, 1);

            var model = new ModelBinding()
            {
                NotificationStaff = notifList,
                PaymentPagination = pagination1
            };

            return View("~/Views/Staff/Cashier1/OfflineDashboard.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetBill(ResidentAddress address)
        {
            try
            {
                var resident = await _residentAddress.GetName(address);

                if (resident.Count < 1)
                {
                    return BadRequest("There is no resident in specified address");
                }

                //return View("~/Views/Staff/Waterworks/Index.cshtml");
                var a = Json(resident);
                return a;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> PayBill(Payment payment)
        {
            try
            {
                int result = await _paymentRepository.InsertPaymentOffline(payment);

                if (result < 1)
                {
                    return BadRequest("There was a problem processing the payment. Please try again later.");
                }
                return Ok("Paid Successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Print(Payment payment)
        {
            try
            {
                var dt = await _paymentRepository.PrintWaterBilling(payment);
                if (dt == null || dt.Rows.Count < 1)
                {
                    return BadRequest();
                }

                string mimetype = "";
                int extension = 1;
                var path = $"{this._webHostEnvironment.WebRootPath}\\ReportViewer\\rptPayment.rdlc";

                Dictionary<string, string> parameters = new Dictionary<string, string>();

                LocalReport localReport = new LocalReport(path);
                localReport.AddDataSource("dsreport_payment", dt);

                var result = localReport.Execute(RenderType.Pdf, extension, parameters, mimetype);

                return File(result.MainStream, MediaTypeNames.Application.Octet, "");
                //return File(result.MainStream, "application/pdf", "WaterBillReport.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAs(Payment payment, string fileType)
        {
            try
            {
                if (payment == null || string.IsNullOrEmpty(fileType))
                {
                    return BadRequest("There was an error saving your file.");

                }

                if (payment == null)
                {
                    return BadRequest("No billing data provided.");
                }

                if (string.IsNullOrEmpty(fileType))
                {
                    return BadRequest("File type not specified.");
                }


                var dt = await _paymentRepository.PrintWaterBilling(payment);
                if (dt == null || dt.Rows.Count < 1)
                {
                    return BadRequest("No data found for the given parameters.");
                }

                var fileName = "";
                ReportResult saveFileAs;

                var paymentID = payment.Payment_ID;
                var date = DateTime.Now.ToString("yyyy-MM-dd"); // Simplified date format
                var tmpName = $"PID{paymentID}_{date}";

                var path = $"{this._webHostEnvironment.WebRootPath}\\ReportViewer\\rptPayment.rdlc";
                if (!System.IO.File.Exists(path))
                {
                    return BadRequest("Report definition file not found.");
                }

                LocalReport localReport = new LocalReport(path);
                localReport.AddDataSource("dsreport_payment", dt);

                if (fileType == "pdf")
                {
                    saveFileAs = localReport.Execute(RenderType.Pdf, 1, null);
                    fileName = $"{tmpName}.pdf";
                }
                else if (fileType == "excel")
                {
                    saveFileAs = localReport.Execute(RenderType.Excel, 1, null);
                    fileName = $"{tmpName}.xls";
                }
                else if (fileType == "doc")
                {
                    saveFileAs = localReport.Execute(RenderType.Word, 1, null);
                    fileName = $"{tmpName}.doc";
                }
                else
                {
                    return BadRequest("Invalid file type specified.");
                }

                if (saveFileAs == null || saveFileAs.MainStream == null)
                {
                    return BadRequest("Failed to generate the report file.");
                }


                var memory = new MemoryStream(saveFileAs.MainStream.ToArray());
                memory.Position = 0;


                // Set the correct Content-Disposition header
                var contentDisposition = new System.Net.Mime.ContentDisposition
                {
                    FileName = fileName,
                    Inline = false
                };

                Response.Headers.Add("Content-Disposition", contentDisposition.ToString());

                return File(memory.ToArray(), MediaTypeNames.Application.Octet, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"An error occurred: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> OfflinePaymentPagination(int page_index, string startDate, string endDate)
        {
            try
            {
                // Parse and validate startDate and endDate
                DateTime? parsedStartDate = DateTime.TryParse(startDate, out DateTime resultSD) ? resultSD : (DateTime?)null;
                DateTime? parsedEndDate = DateTime.TryParse(endDate, out DateTime resultED) ? resultED : (DateTime?)null;

                // Fetch total count for pagination
                var numberOfData = await _paymentRepository.GetCountOnlinePayment("offline",
                    parsedStartDate?.ToString("yyyy-MM-dd"),
                    parsedEndDate?.ToString("yyyy-MM-dd"));

                // Initialize and configure pagination
                var pagination = new Pagination<Payment>
                {
                    NumberOfData = numberOfData,
                    ScriptName = "offpagination"
                };
                pagination.set(10, 5, page_index);

                // Fetch paginated data
                pagination.ModelList = await _paymentRepository.GetRecentOnlinePayment(
                    pagination.Offset,
                    10,
                    "offline",
                    parsedStartDate?.ToString("yyyy-MM-dd"),
                    parsedEndDate?.ToString("yyyy-MM-dd"));

                // Prepare the view model
                var viewmodel = new ModelBinding
                {
                    PaymentPagination = pagination
                };

                // Return the appropriate view
                return View("~/Views/Staff/Cashier1/OfflineDashboard.cshtml", viewmodel);
            }
            catch (Exception ex)
            {
                // Log the exception internally (replace with your logging framework)
                Console.Error.WriteLine($"Error in OnlinePaymentPagination: {ex.Message}");

                // Return a generic error message to the client
                return BadRequest("An error occurred while processing your request. Please try again later.");
            }
        }
    }
}
