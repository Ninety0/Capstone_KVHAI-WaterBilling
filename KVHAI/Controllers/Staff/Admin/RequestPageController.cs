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

        public async Task<IActionResult> StatusFilter(string status)
        {
            var model = await _requestDetailsRepository.GetPendingRemovalRequests(status);

            if (model == null || model.Count < 1)
            {
                return BadRequest("No data found with specified status");
            }

            return View("~/Views/Staff/Admin/PageRequest.cshtml", model);//UPDATE TOMMOROW
        }

        [HttpPost]
        public async Task<IActionResult> ApprovePending(string addressID, string name)
        {

            return Ok("Request approved");
        }
    }
}
