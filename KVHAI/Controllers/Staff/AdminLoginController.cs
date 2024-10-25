using KVHAI.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KVHAI.Controllers.Staff
{
    public class AdminLoginController : Controller
    {
        private readonly EmployeeRepository _employeeRepository;

        public AdminLoginController(EmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Staff/Login/Index.cshtml");
        }

        public IActionResult Forgot()
        {
            return View("~/Views/Staff/Login/ForgotPassword.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string user, string pass)
        {
            if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(pass))
            {
                return BadRequest("Complete Fields.");
            }

            var userExist = await _employeeRepository.ValidateAccount(user, pass);

            if (!await _employeeRepository.ValidatePassword(user, pass))
            {
                return BadRequest("Password is incorrect.");
            }

            if (userExist < 1)
            {
                return BadRequest("User not found.");
            }

            var authCredentials = await _employeeRepository.GetEmployeeByUserandPass(user, pass);

            if (authCredentials == null)
            {
                return BadRequest("There was an error accessing your account.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user),
                new Claim(ClaimTypes.NameIdentifier, authCredentials.ID), // You can add roles or other claims here
                new Claim(ClaimTypes.Role, authCredentials.Role)
            };

            var identity = new ClaimsIdentity(claims, "AdminCookieAuth");
            var principal = new ClaimsPrincipal(identity);
            var props = new AuthenticationProperties();

            await HttpContext.SignInAsync("AdminCookieAuth", principal, props);

            var role = authCredentials.Role;
            if (role == "admin")
            {
                return Ok(new { redirectUrl = "/kvhai/staff/admin/dashboard" });
            }
            else if (role == "clerk")
            {
                return Ok(new { redirectUrl = "/kvhai/staff/clerk/home" });
            }
            else if (role == "cashier1")
            {
                return Ok(new { redirectUrl = "/kvhai/staff/cashier-offline/home" });
            }
            else if (role == "cashier2")
            {
                return Ok(new { redirectUrl = "/kvhai/staff/cashier-online/home" });
            }
            else if (role == "waterworks")
            {
                return Ok(new { redirectUrl = "/kvhai/staff/waterworks/home" });
            }

            return Ok(new { redirectUrl = "/kvhai/staff/error" });
        }

    }
}
