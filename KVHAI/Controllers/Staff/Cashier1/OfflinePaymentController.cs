using AspNetCore.Reporting;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace KVHAI.Controllers.Staff.Cashier1
{
    public class OfflinePaymentController : Controller
    {
        private readonly StreetRepository _streetRepository;
        private readonly ResidentAddressRepository _residentAddress;
        private readonly PaymentRepository _paymentRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OfflinePaymentController(StreetRepository streetRepository, ResidentAddressRepository residentAddress, PaymentRepository paymentRepository, IWebHostEnvironment webHostEnvironment)
        {
            _streetRepository = streetRepository;
            _residentAddress = residentAddress;
            _paymentRepository = paymentRepository;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            var streets = await _streetRepository.GetAllStreets();

            return View("~/Views/Staff/Cashier1/OfflinePayment.cshtml", streets);
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
                int result = await _paymentRepository.InsertPayment(payment);

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
        public async Task<IActionResult> Print(ResidentAddress address)
        {
            try
            {
                var dt = await _paymentRepository.PrintWaterBilling(address);
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
    }
}
