using AspNetCore.Reporting;
using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using System.Security.Claims;


namespace KVHAI.Controllers.Staff.Cashier2
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth", Roles = "cashier2")]
    public class OnlinePaymentController : Controller
    {
        private readonly StreetRepository _streetRepository;
        private readonly ResidentAddressRepository _residentAddress;
        private readonly PaymentRepository _paymentRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly NotificationRepository _notification;


        public OnlinePaymentController(StreetRepository streetRepository, ResidentAddressRepository residentAddress, PaymentRepository paymentRepository, IWebHostEnvironment webHostEnvironment, NotificationRepository notification)
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

            var billNoList = await _paymentRepository.GetWaterBillingNumber();

            var pagination1 = new Pagination<Payment>
            {
                ModelList = await _paymentRepository.GetRecentOnlinePayment(0, 10, "online"),
                NumberOfData = await _paymentRepository.GetCountOnlinePayment("online"),
                ScriptName = "onpagination"
            };
            pagination1.set(10, 5, 1);

            var model = new ModelBinding()
            {
                NotificationStaff = notifList,
                PaymentPagination = pagination1
            };

            return View("~/Views/Staff/Cashier2/OnlineDashboard.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> OnlinePaymentPagination(int page_index)
        {
            try
            {
                var pagination2 = new Pagination<Payment>
                {
                    NumberOfData = await _paymentRepository.GetCountOnlinePayment("online"),
                    ScriptName = "onpagination"
                };
                pagination2.set(10, 5, page_index);
                pagination2.ModelList = await _paymentRepository.GetRecentOnlinePayment(pagination2.Offset, 10, "online");

                var viewmodel = new ModelBinding
                {
                    PaymentPagination = pagination2
                };
                return View("~/Views/Staff/Cashier2/OnlineDashboard.cshtml", viewmodel);

                // Use the more explicit method to return the partial view
                //return PartialView("PartialView/_ResidentAccount", pagination2);
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
        public async Task<IActionResult> SaveFile([FromBody] ModelBinding model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("There was an error saving your file.");

                }
                var paymentReport = model.Payment;
                var fileType = model.FileType;

                if (paymentReport == null)
                {
                    return BadRequest("No billing data provided.");
                }

                if (string.IsNullOrEmpty(fileType))
                {
                    return BadRequest("File type not specified.");
                }


                var dt = await _paymentRepository.PrintWaterBilling(paymentReport);
                if (dt == null || dt.Rows.Count < 1)
                {
                    return BadRequest("No data found for the given parameters.");
                }

                var fileName = "";
                ReportResult saveFileAs;

                var paymentID = model.Payment.Payment_ID;
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

    }
}
