using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Admin
{
    public class AdminAccountController : Controller
    {
        private readonly EmployeeRepository _employeeRepository;
        private readonly ResidentRepository _residentRepository;
        private readonly Pagination<Employee> _pagination;
        private readonly IWebHostEnvironment _environment;

        public AdminAccountController(EmployeeRepository employeeRepository, ResidentRepository residentRepository, IWebHostEnvironment environment)
        {
            _employeeRepository = employeeRepository;
            _residentRepository = residentRepository;
            _environment = environment;
            _pagination = new Pagination<Employee>();
        }

        public async Task<IActionResult> Index()
        {
            //var residents = await _residentRepository.GetAllResidentAsync();
            //var viewModel = new ModelBinding
            //{
            //    Residents = residents,
            //    Employees = employee
            //};
            _pagination.ModelList = await _employeeRepository.GetAllEmployeesAsync(0, 20);
            _pagination.NumberOfData = await _employeeRepository.CountEmployeeData();
            _pagination.ScriptName = "pagination_action";
            _pagination.set(10, 10, 1);
            //return PartialView("~/Views/ItemManage/Index.cshtml", page_v1);
            return View("~/Views/Home/Index.cshtml", _pagination);
            //return View("~/Views/Staff/Admin/Account.cshtml", _pagination);
        }

        [HttpPost]
        public async Task<IActionResult> myPagination(int page_index)
        {
            _pagination.NumberOfData = await _employeeRepository.CountEmployeeData();
            _pagination.ScriptName = "pagination_action";
            _pagination.set(20, 10, page_index);
            var offset = _pagination.Offset;
            _pagination.ModelList = await _employeeRepository.GetAllEmployeesAsync(offset, 20);
            return View("~/Views/Home/Index.cshtml", _pagination);

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
        [ValidateAntiForgeryToken]
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
