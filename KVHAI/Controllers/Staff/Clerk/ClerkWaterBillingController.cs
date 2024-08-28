using AspNetCore.Reporting;
using KVHAI.CustomClass;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace KVHAI.Controllers.Staff.Clerk
{
    public class ClerkWaterBillingController : Controller
    {
        private readonly WaterBillingFunction _waterBilling;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ClerkWaterBillingController(WaterBillingFunction waterBillingFunction, IWebHostEnvironment webHostEnvironment)
        {
            _waterBilling = waterBillingFunction;
            _webHostEnvironment = webHostEnvironment;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public async Task<IActionResult> Index()
        {
            //return View("ClerkWaterBilling");
            await _waterBilling.UseWaterBilling("1");

            return View("~/Views/Staff/Clerk/WBilling.cshtml", _waterBilling);
            //return View("~/Views/Staff/Clerk/Index.cshtml", _waterBilling);
        }

        public async Task<IActionResult> Print()
        {
            string mimetype = "";
            int extension = 1;
            var path = $"{this._webHostEnvironment.WebRootPath}\\ReportViewer\\rptWaterBill.rdlc";

            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("wb", "Water Billing");
            LocalReport localReport = new LocalReport(path);

            var result = localReport.Execute(RenderType.Pdf, extension, parameters, mimetype);

            return File(result.MainStream, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> WaterBillingLocation(string location = "", string waterBill = "")
        {
            try
            {
                await _waterBilling.UseWaterBilling(location: location, wbnumber: waterBill);
                string jsonData = JsonConvert.SerializeObject(View("~/Views/Staff/Clerk/WBilling.cshtml", _waterBilling).ToString());
                //return Ok(jsonData);
                //return Ok($"WaterBillingLocation called with location: {location}, waterBill: {waterBill}");
                return View("~/Views/Staff/Clerk/WBilling.cshtml", _waterBilling);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> WaterBillingByNumber(string location = "", string waterBill = "")
        {
            try
            {

                await _waterBilling.UseWaterBilling("1");
                return View("~/Views/Staff/Clerk/WBilling.cshtml", _waterBilling);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
