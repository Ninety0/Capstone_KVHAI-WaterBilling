using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Admin
{
    public class ResidentAddressController : Controller
    {
        private readonly EmployeeRepository _employeeRepository;
        private readonly ResidentRepository _residentRepository;
        private readonly AddressRepository _addressRepository;
        private readonly IWebHostEnvironment _environment;


        public ResidentAddressController(ResidentRepository residentRepository, EmployeeRepository employeeRepository, AddressRepository addressRepository, IWebHostEnvironment environment)
        {
            _residentRepository = residentRepository;
            _employeeRepository = employeeRepository;
            _addressRepository = addressRepository;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            //RESIDENT
            var pagination2 = new Pagination<AddressWithResident>
            {
                ModelList = await _residentRepository.GetAllResidentAsync(0, 10, "false"),
                NumberOfData = await _residentRepository.CountResidentData("false"),
                ScriptName = "respagination"
            };
            pagination2.set(10, 5, 1);

            return View("~/Views/Staff/Admin/ResidentAddress.cshtml", pagination2);
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestAddress()
        {
            //RESIDENT
            var pagination2 = new Pagination<AddressWithResident>
            {
                ModelList = await _residentRepository.GetAllResidentAsync(0, 10, "false"),
                NumberOfData = await _residentRepository.CountResidentData("false"),
                ScriptName = "respagination"
            };
            pagination2.set(10, 5, 1);

            return View("~/Views/Staff/Admin/ResidentAddress.cshtml", pagination2);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int addrID, string status)
        {
            try
            {
                int result = await _addressRepository.UpdateAddressStatus(addrID, status);
                if (result < 1)
                {
                    return BadRequest("There was an error approving the status");
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return BadRequest("There was an error processing the request.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetImageBase64(string id)
        {
            var imagePath = await _residentRepository.GetImagePathAsync(id);
            if (string.IsNullOrEmpty(imagePath))
                return Json(new { success = false });

            var fullPath = Path.Combine(_environment.WebRootPath, imagePath);
            if (!System.IO.File.Exists(fullPath))
                return Json(new { success = false });

            byte[] imageArray = await System.IO.File.ReadAllBytesAsync(fullPath);
            string base64ImageRepresentation = Convert.ToBase64String(imageArray);

            return Json(new { success = true, imageBase64 = base64ImageRepresentation });
        }

        [HttpPost]
        public async Task<IActionResult> ResidentPagination(string search, string category, string is_verified, int page_index)
        {
            try
            {
                var resSearch = search == null || string.IsNullOrEmpty(search) ? "" : search;

                //RESIDENT
                var pagination2 = new Pagination<AddressWithResident>
                {
                    NumberOfData = await _residentRepository.CountResidentData(is_verified, category, resSearch),
                    ScriptName = "respagination"
                };
                pagination2.set(10, 5, page_index);
                pagination2.ModelList = await _residentRepository.GetAllResidentAsync(pagination2.Offset, 10, is_verified, category, resSearch);

                return View("~/Views/Staff/Admin/ResidentAddress.cshtml", pagination2);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
