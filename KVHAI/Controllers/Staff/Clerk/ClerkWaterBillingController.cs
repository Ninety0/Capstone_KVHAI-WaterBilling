using AspNetCore.Reporting;
using KVHAI.CustomClass;
using KVHAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Mime;

namespace KVHAI.Controllers.Staff.Clerk
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth", Roles = "clerk")]
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
            var billNumList = await _waterBilling.GetWaterBillNumberList();

            //await _waterBilling.WaterBilling(location: "1", wbnumber: billNumList.FirstOrDefault());
            return View("~/Views/Staff/Clerk/WBilling.cshtml", _waterBilling);
            //return View("~/Views/Staff/Clerk/Index.cshtml", _waterBilling);
        }

        [HttpGet]
        public async Task<IActionResult> GetWaterBills(string location, string bill_num)
        {
            //await _waterBilling.WaterBilling(location: location, wbnumber: bill_num);
            await _waterBilling.GetWaterBillingValues(location, bill_num);
            return View("~/Views/Staff/Clerk/WBilling.cshtml", _waterBilling);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFile([FromBody] ModelBinding modelBinding)
        {
            try
            {
                var reportWaterBilling = modelBinding.Items;
                var fileType = modelBinding.FileType;

                if (reportWaterBilling == null || reportWaterBilling.Count == 0)
                {
                    return BadRequest("No billing data provided.");
                }

                if (string.IsNullOrEmpty(fileType))
                {
                    return BadRequest("File type not specified.");
                }

                var dt = await _waterBilling.PrintWaterBilling(reportWaterBilling);
                if (dt == null || dt.Rows.Count < 1)
                {
                    return BadRequest("No data found for the given parameters.");
                }

                var wbNumber = reportWaterBilling.FirstOrDefault()?.WaterBill_Number;
                var date = DateTime.Now.ToString("yyyy-MM-dd"); // Simplified date format
                var tmpName = $"WB{wbNumber}_{date}";
                var fileName = "";
                ReportResult saveFileAs;

                var path = $"{this._webHostEnvironment.WebRootPath}\\ReportViewer\\rptWaterBill.rdlc";
                if (!System.IO.File.Exists(path))
                {
                    return BadRequest("Report definition file not found.");
                }

                LocalReport localReport = new LocalReport(path);
                localReport.AddDataSource("dsreport_waterbill", dt);

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

                //var savePath = Path.Combine(this._webHostEnvironment.WebRootPath, "GeneratedReports", fileName);
                //Directory.CreateDirectory(Path.GetDirectoryName(savePath)); // Ensure the directory exists
                //await System.IO.File.WriteAllBytesAsync(savePath, saveFileAs.MainStream.ToArray());

                var memory = new MemoryStream(saveFileAs.MainStream.ToArray());
                memory.Position = 0;

                //if (System.IO.File.Exists(savePath))
                //{
                //    var net = new System.Net.WebClient();
                //    var data = net.DownloadData(savePath);
                //    var content = new System.IO.MemoryStream(data);
                //    memory = content;
                //}

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
        public async Task<IActionResult> Print([FromBody] List<ReportWaterBilling> reportWaterBilling)
        {
            try
            {
                var dt = await _waterBilling.PrintWaterBilling(reportWaterBilling);
                if (dt == null || dt.Rows.Count < 1)
                {
                    return BadRequest();
                }

                string mimetype = "";
                int extension = 1;
                var path = $"{this._webHostEnvironment.WebRootPath}\\ReportViewer\\rptWaterBill.rdlc";

                Dictionary<string, string> parameters = new Dictionary<string, string>();

                LocalReport localReport = new LocalReport(path);
                localReport.AddDataSource("dsreport_waterbill", dt);

                var result = localReport.Execute(RenderType.Pdf, extension, parameters, mimetype);

                return File(result.MainStream, MediaTypeNames.Application.Octet, "");
                //return File(result.MainStream, "application/pdf", "WaterBillReport.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        public async Task<IActionResult> WaterBillingLocation(string location = "", string waterBill = "")
        {
            try
            {
                //await _waterBilling.WaterBilling(location: location, wbnumber: waterBill);
                await _waterBilling.GetWaterBillingValues(location, waterBill);
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

                await _waterBilling.WaterBilling("1");
                return View("~/Views/Staff/Clerk/WBilling.cshtml", _waterBilling);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
