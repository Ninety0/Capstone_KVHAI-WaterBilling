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
        private readonly ResidentAddressRepository _residentAddressRepository;
        private readonly AddressRepository _addressRepository;


        public BillingController(WaterBillingFunction waterBillingFunction, WaterBillRepository waterBillRepository, NotificationRepository notification, ResidentAddressRepository residentAddressRepository, AddressRepository addressRepository)
        {
            _waterBillingFunction = waterBillingFunction;
            _waterBillRepo = waterBillRepository;
            _notification = notification;
            _residentAddressRepository = residentAddressRepository;
            _addressRepository = addressRepository;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var notifList = await _notification.GetNotificationByResident(residentID);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            int addressID = 0;

            var address = new List<Models.Address>();

            if (role == "1")
            {
                address = await _addressRepository.GetAddressessByResId(residentID);
            }
            else
            {
                address = await _residentAddressRepository.GetAddressessByResId(residentID);
            }

            var viewModel = new ModelBinding
            {
                ListAddress = address,
                NotificationResident = notifList,
            };
            //await _waterBillingFunction.UnpaidWaterBillingByResident("1");
            return View("~/Views/Resident/LoggedIn/Bills.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetBilling(string addressID)
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //var notifList = await _notification.GetNotificationByResident(residentID);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var unpaidBill = new List<WaterBillWithAddress>();
            var paidBill = new List<WaterBillWithAddress>();

            if (role == "1")
            {
                unpaidBill = await _waterBillRepo.UnpaidResidentWaterBilling(residentID, addressID);
                paidBill = await _waterBillRepo.GetPaidResidentWaterBilling(residentID, addressID);
            }
            else
            {
                unpaidBill = await _residentAddressRepository.UnpaidResidentWaterBilling(residentID, addressID);
                paidBill = await _residentAddressRepository.GetPaidResidentWaterBilling(residentID, addressID);
            }

            var viewModel = new ModelBinding
            {
                UnpaidResidentWaterBilling = unpaidBill,
                PaidResidentWaterBilling = paidBill
            };
            return View("~/Views/Resident/LoggedIn/Bills.cshtml", viewModel);
        }

        //[HttpGet]
        //public async Task<IActionResult> GetNewBills()
        //{
        //    var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    var notifList = await _notification.GetNotificationByResident(residentID);
        //    var role = User.FindFirst(ClaimTypes.Role)?.Value;

        //    var model = new List<WaterBillWithAddress>();

        //    if (role == "1")
        //    {
        //        model = await _waterBillRepo.UnpaidResidentWaterBilling(residentID);
        //    }
        //    else
        //    {
        //        model = await _residentAddressRepository.UnpaidResidentWaterBilling(residentID);
        //    }


        //    var viewModel = new ModelBinding
        //    {
        //        NotificationResident = notifList,
        //        UnpaidResidentWaterBilling = model
        //    };
        //    //await _waterBillingFunction.UnpaidWaterBillingByResident("1");
        //    return View("~/Views/Resident/LoggedIn/Bills.cshtml", viewModel);
        //}
    }
}
