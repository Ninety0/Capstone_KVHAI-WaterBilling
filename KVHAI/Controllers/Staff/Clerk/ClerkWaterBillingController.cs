using KVHAI.CustomClass;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace KVHAI.Controllers.Staff.Clerk
{
    public class ClerkWaterBillingController : Controller
    {
        private readonly WaterBillingFunction _waterBilling;

        public ClerkWaterBillingController(WaterBillingFunction waterBillingFunction)
        {
            _waterBilling = waterBillingFunction;
        }

        public async Task<IActionResult> Index()
        {
            //return View("ClerkWaterBilling");
            await _waterBilling.UseWaterBilling("1");

            return View("~/Views/Staff/Clerk/WBilling.cshtml", _waterBilling);
            //return View("~/Views/Staff/Clerk/Index.cshtml", _waterBilling);
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
