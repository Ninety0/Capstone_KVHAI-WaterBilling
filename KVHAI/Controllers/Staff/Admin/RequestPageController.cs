using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Admin
{
    public class RequestPageController : Controller
    {
        private readonly AddressRepository _addressRepository;
        private readonly RequestDetailsRepository _requestDetailsRepository;

        public RequestPageController(AddressRepository addressRepository, RequestDetailsRepository requestDetailsRepository)
        {
            _addressRepository = addressRepository;
            _requestDetailsRepository = requestDetailsRepository;
        }
        public async Task<IActionResult> Index()
        {
            var model = await _requestDetailsRepository.GetPendingRemovalRequests();

            return View("~/Views/Staff/Admin/PageRequest.cshtml", model);//UPDATE TOMMOROW
        }

        [HttpGet]
        public async Task<IActionResult> GetNewRequest()
        {
            var model = await _requestDetailsRepository.GetPendingRemovalRequests();

            return View("~/Views/Staff/Admin/PageRequest.cshtml", model);//UPDATE TOMMOROW
        }

        public async Task<IActionResult> StatusFilter(string status = "", string date = "")
        {
            var model = await _requestDetailsRepository.GetPendingRemovalRequests(status, date);

            if (model == null)
            {
                return BadRequest("No data found with specified status");
            }

            return View("~/Views/Staff/Admin/PageRequest.cshtml", model);//UPDATE TOMMOROW
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
