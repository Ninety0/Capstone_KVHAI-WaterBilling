using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace KVHAI.Controllers.Staff
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth")]
    public class RemittanceController : Controller
    {
        private readonly NotificationRepository _notification;
        private readonly PaymentRepository _paymentRepository;

        public RemittanceController(NotificationRepository notification, PaymentRepository paymentRepository)
        {
            _notification = notification;
            _paymentRepository = paymentRepository;
        }
        public async Task<IActionResult> Index()
        {
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;
            var notifList = await _notification.GetNotificationByStaff(role);

            var model = new ModelBinding()
            {
                NotificationStaff = notifList
            };
            return View("~/Views/Staff/Cashier2/Remittance.cshtml", model);

        }

        [HttpGet]
        public async Task<IActionResult> GetPayments(string date)
        {
            var payment = await _paymentRepository.GetNewPayment(date);
            var modelBinding = new ModelBinding
            {
                PaymentList = payment,
            };
            if (payment == null)
            {
                return BadRequest("Null");
            }

            return View("~/Views/Staff/Cashier2/Remittance.cshtml", modelBinding);

        }
    }
}
