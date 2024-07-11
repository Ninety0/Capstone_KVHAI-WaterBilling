using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Admin
{
    public class AdminAccountController : Controller
    {
        private readonly EmployeeRepository _employeeRepository;
        private readonly ResidentRepository _residentRepository;
        private readonly IWebHostEnvironment _environment;

        public AdminAccountController(EmployeeRepository employeeRepository, ResidentRepository residentRepository, IWebHostEnvironment environment)
        {
            _employeeRepository = employeeRepository;
            _residentRepository = residentRepository;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var residents = await _residentRepository.GetAllResidentAsync();
            var employee = await _employeeRepository.GetAllEmployeesAsync();
            var viewModel = new ModelBinding
            {
                Residents = residents,
                Employees = employee
            };
            return View("~/Views/Staff/Admin/Account.cshtml", viewModel);
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
    }
}
