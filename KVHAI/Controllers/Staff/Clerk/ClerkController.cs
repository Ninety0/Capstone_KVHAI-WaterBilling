using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Clerk
{
    public class ClerkController : Controller
    {
        //private readonly WaterReadingRepository _waterReadingRepository;
        private readonly WaterBillingFunction _waterBilling;
        private readonly WaterBillRepository _waterBillRepository;

        public ClerkController(WaterBillingFunction waterBilling, WaterBillRepository waterBillRepository)//WaterReadingRepository waterReadingRepository
        {
            _waterBilling = waterBilling;
            _waterBillRepository = waterBillRepository;
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

            await _waterBilling.UseWaterBilling("1");

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
                await _waterBilling.UseWaterBilling(location, fromDate, toDate);

                return View("~/Views/Staff/Clerk/Index.cshtml", _waterBilling);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> WaterReadingByMonth(string fromDate, string toDate)
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
                await _waterBilling.UseWaterBilling("1", fromDate, toDate);

                return View("~/Views/Staff/Clerk/Index.cshtml", _waterBilling);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBilling([FromBody] ModelBinding binding)//int id, string amount, string date)
        {
            try
            {
                var ListBilling = new List<WaterBilling>();
                var dateArgument = binding.Date + DateTime.Now.ToString("-yyyy");
                var billing = await _waterBillRepository.GetDateBilling(dateArgument);
                var due = await _waterBillRepository.GetDueDate();
                var status = "unpaid";

                foreach (var item in binding.Bill)
                {
                    if (item == null)
                    {
                        return BadRequest("There was an error processing the data.");
                    }

                    var wb = new WaterBilling()
                    {
                        Reading_ID = item.Reading_ID,
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
    }
}
