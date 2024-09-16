using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Admin
{
    public class RequestPageController : Controller
    {
        private readonly AddressRepository _addressRepository;

        public RequestPageController(AddressRepository addressRepository)
        {
            _addressRepository = addressRepository;
        }
        public async Task<IActionResult> Index()
        {
            //var model = await _addressRepository.GetPendingRemovalRequests();

            return View("~/Views/Staff/Admin/PageRequest.cshtml", model);//UPDATE TOMMOROW
        }

        [HttpPost]
        public async Task<IActionResult> ApprovePending(string addressID, string name)
        {

            return Ok("Request approved");
        }
    }
}
