﻿using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace KVHAI.Controllers.Staff.Admin
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth", Roles = "admin")]
    public class AdminAccountController : Controller
    {
        private readonly EmployeeRepository _employeeRepository;
        private readonly ResidentRepository _residentRepository;
        private readonly AddressRepository _addressRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly NotificationRepository _notification;
        private readonly StreetRepository _streetRepository;



        public AdminAccountController(EmployeeRepository employeeRepository, ResidentRepository residentRepository, AddressRepository addressRepository, IWebHostEnvironment environment, NotificationRepository notification, StreetRepository streetRepository)
        {
            _employeeRepository = employeeRepository;
            _residentRepository = residentRepository;
            _addressRepository = addressRepository;
            _environment = environment;
            _notification = notification;
            _streetRepository = streetRepository;
        }

        public async Task<IActionResult> Index()
        {
            var listStreet = await _streetRepository.GetAllStreets();
            var empID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.Identity.Name;

            var notifList = await _notification.GetNotificationByStaff(role);

            //EMPLOYEE
            var pagination1 = new Pagination<Employee>
            {
                ModelList = await _employeeRepository.GetAllEmployeesAsync(0, 10),
                NumberOfData = await _employeeRepository.CountEmployeeData(),
                ScriptName = "emppagination"
            };
            pagination1.set(pagination1.ModelList.Count, 5, 1);

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
                EmployeePagination = pagination1,
                ResidentPagination = pagination2,
                NotificationStaff = notifList,
                ListStreet = listStreet
            };
            return View("~/Views/Staff/Admin/Account.cshtml", viewmodel);
        }


        [HttpPost]
        public async Task<IActionResult> EmployeePagination(string search, string category, int page_index)
        {
            try
            {
                var empSearch = search == null || string.IsNullOrEmpty(search) ? "" : search;
                //EMPLOYEE
                var pagination1 = new Pagination<Employee>
                {
                    NumberOfData = await _employeeRepository.CountEmployeeData(category, empSearch),
                    ScriptName = "emppagination"
                };
                pagination1.set(10, 5, page_index);
                pagination1.ModelList = await _employeeRepository.GetAllEmployeesAsync(empSearch, category, pagination1.Offset, 10);

                //RESIDENT
                var pagination2 = new Pagination<AddressWithResident>
                {
                    ModelList = await _residentRepository.GetAllResidentAsync(0, 10),
                    NumberOfData = await _residentRepository.CountResidentData("false"),
                    ScriptName = "respagination"
                };
                pagination2.set(10, 5, 1);

                var viewmodel = new ModelBinding
                {
                    EmployeePagination = pagination1,
                    ResidentPagination = pagination2
                };

                return View("~/Views/Staff/Admin/Account.cshtml", viewmodel);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> ResidentPagination(string search, string is_verified, int page_index)
        {
            try
            {
                var listStreet = await _streetRepository.GetAllStreets();

                //EMPLOYEE
                var pagination1 = new Pagination<Employee>
                {
                    ModelList = await _employeeRepository.GetAllEmployeesAsync(0, 10),
                    NumberOfData = await _employeeRepository.CountEmployeeData(),
                    ScriptName = "emppagination"
                };
                pagination1.set(pagination1.ModelList.Count, 5, 1);

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
                    EmployeePagination = pagination1,
                    ResidentPagination = pagination2,
                    ListStreet = listStreet

                };
                return View("~/Views/Staff/Admin/Account.cshtml", viewmodel);

                // Use the more explicit method to return the partial view
                //return PartialView("PartialView/_ResidentAccount", pagination2);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterEmployee(Employee formData)
        {
            try
            {
                if (await _employeeRepository.UserExists(formData))
                {
                    return Ok(new { message = "exist" });
                }

                int result = await _employeeRepository.CreateEmployee(formData);

                if (result == 0)
                    return BadRequest(new { message = "There was an error saving the resident and the image." });

                //return Ok(new { message = "Registration Successful." });
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return BadRequest(new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RegisterResident(Resident resident, string addresses)
        {
            try
            {
                var addressDataList = JsonConvert.DeserializeObject<List<Address>>(addresses);
                var modelAddressList = new List<Address>();

                var stIDList = await _streetRepository.GetStreetID(addressDataList);


                for (int i = 0; i < addressDataList.Count(); i++)
                {
                    // Check if there is already an entry with the same lot and street name in the modelAddressList
                    bool isDuplicate = modelAddressList.Any(a => a.Lot == addressDataList[i].Lot && a.Street_Name == addressDataList[i].Street_Name);

                    // If it's not a duplicate, process the address and its associated file
                    if (!isDuplicate)
                    {
                        var addressModel = new Address
                        {
                            Street_ID = stIDList[i].Street_ID,
                            Block = addressDataList[i].Block,
                            Lot = addressDataList[i].Lot,
                            Street_Name = addressDataList[i].Street_Name,
                            Date_Residency = addressDataList[i].Date_Residency
                        };

                        var isAddressExist = await _addressRepository.IsAddressExist(addressDataList[i].Block, addressDataList[i].Lot);

                        if (isAddressExist)
                        {
                            return BadRequest("The address was already been registered.");

                        }
                        modelAddressList.Add(addressModel);
                    }
                }


                var insertResident = await _residentRepository.CreateResident(resident, modelAddressList);

                if (insertResident < 1)
                {
                    return BadRequest("There was an error in processing the data.");
                }

                //if (await _employeeRepository.UserExists(formData))
                //{
                //    return Ok(new { message = "exist" });
                //}

                //int result = await _employeeRepository.CreateEmployee(formData);

                //if (result == 0)
                //    return BadRequest(new { message = "There was an error saving the resident and the image." });

                return Ok(new { message = "Added Successful." });
                //return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return BadRequest(new { message = "An error occurred while processing your request." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmployee(Employee formData)
        {
            try
            {

                int result = await _employeeRepository.UpdateEmployee(formData);

                if (result == 0)
                    return BadRequest(new { message = "There was an error updating credentials" });

                //return Ok(new { message = "Registration Successful." });
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return BadRequest(new { message = "An error occurred while processing your request." });
            }
        }



        [HttpGet]
        public async Task<IActionResult> GetEmployees(string id)
        {
            var employee = await _employeeRepository.GetSingleEmployee(id);
            if (employee == null)
            {
                return NotFound();
            }
            return Ok(employee);
        }



        [HttpDelete]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _employeeRepository.DeleteEmployee(id);
            if (employee == 0)
            {
                return BadRequest(new { message = "error" });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
