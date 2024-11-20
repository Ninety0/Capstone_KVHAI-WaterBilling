using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace KVHAI.Controllers.Staff.Admin
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth", Roles = "admin")]
    public class RequestPageController : Controller
    {
        private readonly AddressRepository _addressRepository;
        private readonly RequestDetailsRepository _requestDetailsRepository;
        private readonly NotificationRepository _notification;


        public RequestPageController(AddressRepository addressRepository, RequestDetailsRepository requestDetailsRepository, NotificationRepository notification)
        {
            _addressRepository = addressRepository;
            _requestDetailsRepository = requestDetailsRepository;
            _notification = notification;
        }
        public async Task<IActionResult> Index()
        {
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;

            var notifList = await _notification.GetNotificationByStaff(role);

            var model = await _requestDetailsRepository.GetPendingRemovalRequests();


            var viewmodel = new ModelBinding
            {
                NotificationStaff = notifList,
                RequestDetailList = model

            };


            return View("~/Views/Staff/Admin/PageRequest.cshtml", viewmodel);//UPDATE TOMMOROW
        }

        [HttpGet]
        public async Task<IActionResult> GetNewRequest()
        {
            var model = await _requestDetailsRepository.GetPendingRemovalRequests();

            var viewmodel = new ModelBinding
            {
                RequestDetailList = model

            };

            return View("~/Views/Staff/Admin/PageRequest.cshtml", viewmodel);//UPDATE TOMMOROW
        }

        public async Task<IActionResult> StatusFilter(string status = "", string date = "")
        {
            var model = await _requestDetailsRepository.GetPendingRemovalRequests(status, date);

            if (model == null)
            {
                return BadRequest("No data found with specified status");
            }

            var viewmodel = new ModelBinding
            {
                RequestDetailList = model

            };

            return View("~/Views/Staff/Admin/PageRequest.cshtml", viewmodel);//UPDATE TOMMOROW
        }

        [HttpPost]
        public async Task<IActionResult> ApprovePending(RequestDetails request)
        {
            var result = await _requestDetailsRepository.UpdateRequestStatus(request);

            if (result < 1)
            {
                return BadRequest("There was an error processing the request!");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
