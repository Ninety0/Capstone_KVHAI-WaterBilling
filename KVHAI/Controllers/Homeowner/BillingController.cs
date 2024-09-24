using KVHAI.CustomClass;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Homeowner
{
    public class BillingController : Controller
    {
        private readonly WaterBillingFunction _waterBillingFunction;
        private readonly WaterBillRepository _waterBillRepo;

        public BillingController(WaterBillingFunction waterBillingFunction, WaterBillRepository waterBillRepository)
        {
            _waterBillingFunction = waterBillingFunction;
            _waterBillRepo = waterBillRepository;
        }

        public async Task<IActionResult> Index()
        {
            //await _waterBillingFunction.UnpaidWaterBillingByResident("1");
            var model = await _waterBillRepo.UnpaidResidentWaterBilling("1");
            return View("~/Views/Resident/Billing/Bills.cshtml", model);
        }
    }
}
