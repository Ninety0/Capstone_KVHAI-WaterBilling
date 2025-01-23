using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KVHAI.Controllers.Staff.Admin
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth", Roles = "admin")]
    public class ResidentAddressController : Controller
    {
        private readonly EmployeeRepository _employeeRepository;
        private readonly ResidentRepository _residentRepository;
        private readonly AddressRepository _addressRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly NotificationRepository _notification;
        private readonly StreetRepository _streetRepository;
        private readonly ListRepository _listRepository;

        public ResidentAddressController(ResidentRepository residentRepository, EmployeeRepository employeeRepository, AddressRepository addressRepository, IWebHostEnvironment environment, NotificationRepository notification
            , StreetRepository streetRepository, ListRepository listRepository)
        {
            _residentRepository = residentRepository;
            _employeeRepository = employeeRepository;
            _addressRepository = addressRepository;
            _environment = environment;
            _notification = notification;
            _streetRepository = streetRepository;
            _listRepository = listRepository;
        }

        public async Task<IActionResult> Index()
        {
            var listStreet = await _streetRepository.GetAllStreets();
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;

            var notifList = await _notification.GetNotificationByStaff(role);

            //RESIDENT
            var pagination2 = new Pagination<AddressWithResident>
            {
                ModelList = await _residentRepository.GetAllResidentAsyncAccount(0, 10),
                NumberOfData = await _residentRepository.CountResidentData("false"),
                ScriptName = "respagination"
            };
            pagination2.set(10, 5, 1);

            var viewmodel = new ModelBinding
            {
                ResidentPagination = pagination2,
                NotificationStaff = notifList,
                ListStreet = listStreet
            };

            return View("~/Views/Staff/Admin/ResidentAddress.cshtml", viewmodel);
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestAddress()
        {
            var listStreet = await _streetRepository.GetAllStreets();
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;

            var notifList = await _notification.GetNotificationByStaff(role);

            //RESIDENT
            var pagination2 = new Pagination<AddressWithResident>
            {
                ModelList = await _residentRepository.GetAllResidentAsyncAccount(0, 10),
                NumberOfData = await _residentRepository.CountResidentData("false"),
                ScriptName = "respagination"
            };
            pagination2.set(10, 5, 1);

            var viewmodel = new ModelBinding
            {
                ResidentPagination = pagination2,
                NotificationStaff = notifList,
                ListStreet = listStreet
            };

            return View("~/Views/Staff/Admin/ResidentAddress.cshtml", viewmodel);
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
        public async Task<IActionResult> ResidentPagination(string search, int page_index)
        {
            try
            {
                var listStreet = await _streetRepository.GetAllStreets();

                //RESIDENT
                var resSearch = search == null || string.IsNullOrEmpty(search) ? "" : search;

                var pagination2 = new Pagination<AddressWithResident>
                {
                    NumberOfData = await _residentRepository.CountResidentDataAccount(resSearch),
                    ScriptName = "respagination"
                };
                pagination2.set(10, 5, page_index);
                pagination2.ModelList = await _residentRepository.GetAllResidentAsyncAccount(pagination2.Offset, 10, resSearch);

                var viewmodel = new ModelBinding
                {
                    ResidentPagination = pagination2,
                    ListStreet = listStreet

                };

                return View("~/Views/Staff/Admin/ResidentAddress.cshtml", viewmodel);


                // Use the more explicit method to return the partial view
                //return PartialView("PartialView/_ResidentAccount", pagination2);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetResident(string id)
        {
            // Fetch data from repositories
            var resident = await _listRepository.ResidentList();
            var street = await _listRepository.StreetList();
            var address = await _listRepository.AddressList();

            // Find resident and address accounts
            var residentAccount = resident.Where(r => r.Res_ID == id).ToList();
            var addressAccount = address.Where(r => r.Resident_ID == Convert.ToInt32(id)).ToList();

            // Check if addressAccount is not empty before accessing FirstOrDefault
            if (addressAccount.Any())
            {
                // Find the street name
                var streetName = street
                    .Where(i => i.Street_ID == addressAccount.FirstOrDefault().Street_ID.ToString())
                    .Select(n => n.Street_Name)
                    .FirstOrDefault();

                // Add street name to the address objects
                foreach (var addr in addressAccount)
                {
                    addr.Street_Name = streetName; // Assuming Address object has a Street_Name property
                }
            }

            // Handle case where residentAccount is empty
            if (!residentAccount.Any())
            {
                return NotFound();
            }

            // Return the data
            return Ok(new
            {
                ResidentAccount = residentAccount,
                AddressAccount = addressAccount
            });

        }

        [HttpPost]
        public async Task<IActionResult> UpdateResident(int res_id, string lname, string fname, string mname, int addr_id, string block, string lot, string street_Name, DateTime date_Residency)
        {
            // Perform update logic here
            // Example: Update resident and address in the database

            //var isAddressExist = await _addressRepository.IsAddressExist(block, lot);

            //if (isAddressExist)
            //{
            //    return BadRequest("The address was already been registered.");

            //}



            return Ok(new { message = "Resident updated successfully!" });
        }

        //[HttpPost]
        //public async Task<IActionResult> ResidentPagination(string search, string category, string is_verified, int page_index)
        //{
        //    try
        //    {
        //        var resSearch = search == null || string.IsNullOrEmpty(search) ? "" : search;

        //        //RESIDENT
        //        var pagination2 = new Pagination<AddressWithResident>
        //        {
        //            NumberOfData = await _residentRepository.CountResidentData(is_verified, category, resSearch),
        //            ScriptName = "respagination"
        //        };
        //        pagination2.set(10, 5, page_index);
        //        pagination2.ModelList = await _residentRepository.GetAllResidentAsync(pagination2.Offset, 10, is_verified, category, resSearch);

        //        var viewmodel = new ModelBinding
        //        {
        //            ResidentPagination = pagination2
        //        };

        //        return View("~/Views/Staff/Admin/ResidentAddress.cshtml", viewmodel);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}
    }
}
