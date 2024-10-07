using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KVHAI.Controllers.Homeowner
{
    public class BillingController : Controller
    {
        private readonly WaterBillingFunction _waterBillingFunction;
        private readonly WaterBillRepository _waterBillRepo;
        private readonly NotificationRepository _notification;

        public BillingController(WaterBillingFunction waterBillingFunction, WaterBillRepository waterBillRepository, NotificationRepository notification)
        {
            _waterBillingFunction = waterBillingFunction;
            _waterBillRepo = waterBillRepository;
            _notification = notification;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var notifList = await _notification.GetNotificationByResident(residentID);
            var model = await _waterBillRepo.UnpaidResidentWaterBilling("1");


            var viewModel = new ModelBinding
            {
                NotificationResident = notifList,
                UnpaidResidentWaterBilling = model
            };
            //await _waterBillingFunction.UnpaidWaterBillingByResident("1");
            return View("~/Views/Resident/Billing/Bills.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetNewBills()
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var notifList = await _notification.GetNotificationByResident(residentID);
            var model = await _waterBillRepo.UnpaidResidentWaterBilling("1");


            var viewModel = new ModelBinding
            {
                NotificationResident = notifList,
                UnpaidResidentWaterBilling = model
            };
            //await _waterBillingFunction.UnpaidWaterBillingByResident("1");
            return View("~/Views/Resident/Billing/Bills.cshtml", viewModel);
        }
    }
}
