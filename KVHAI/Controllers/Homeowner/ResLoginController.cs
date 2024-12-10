using KVHAI.CustomClass;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KVHAI.Controllers.Homeowner

{
    public class ResLoginController : Controller
    {
        private readonly ResidentRepository _residentRepository;
        private readonly StreetRepository _streetRepository;
        private readonly AddressRepository _addressRepository;
        private readonly ImageUploadRepository _imageRepository;
        private readonly LoginRepository _loginRepository;
        private readonly InputSanitize _sanitize;
        private readonly IWebHostEnvironment _environment;


        public ResLoginController(ResidentRepository residentRepository, StreetRepository streetRepository, AddressRepository addressRepository, InputSanitize sanitize, ImageUploadRepository imageUpload, LoginRepository loginRepository, IWebHostEnvironment environment)
        {
            _residentRepository = residentRepository;
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
            _sanitize = sanitize;
            _imageRepository = imageUpload;
            _loginRepository = loginRepository;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View("~/Views/Resident/RLogin/Index.cshtml");
        }

        public async Task<IActionResult> Signup()
        {
            try
            {
                var st = await _streetRepository.GetAllStreets();
                return View("~/Views/Resident/Signup/Index.cshtml", st);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Resident credentials)
        {
            var userExist = await _residentRepository.IsUsernameExist(credentials.Username);

            if (!userExist)
            {
                return BadRequest("User not found.");
            }

            if (!await _residentRepository.ValidatePassword(credentials))
            {
                return BadRequest("Password is incorrect.");
            }




            //if (string.IsNullOrEmpty(verifiedAt[0].Verified_At))
            //{
            //    return Ok(new { message = "Account is not verified!", token = verifiedAt[0].Verification_Token, email = verifiedAt[0].Email });
            //}

            var authCredentials = await _residentRepository.GetResidentID(credentials);

            if (authCredentials == null)
            {
                return BadRequest("There was an error accessing your account.");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, credentials.Username),
                new Claim(ClaimTypes.NameIdentifier, authCredentials.ID), // You can add roles or other claims here
                new Claim(ClaimTypes.Role, authCredentials.Role)
            };

            var identity = new ClaimsIdentity(claims, "MyCookieAuth");
            var principal = new ClaimsPrincipal(identity);
            var props = new AuthenticationProperties();

            await HttpContext.SignInAsync("MyCookieAuth", principal, props);

            var role = authCredentials.Role;
            if (role == "1")
            {
                return Ok(new { redirectUrl = "/kvhai/resident/announcement" });
            }
            else
            {
                return Ok(new { redirectUrl = "/kvhai/resident/announcement" });
            }
            //return Ok(new { redirectUrl = Url.Action("LoggedIn", "LoggedIn") });
            //ViewData["Username"] = credentials.Username;

            //return RedirectToAction("LoggedIn");
        }


        [HttpPost]
        public async Task<IActionResult> VerifyPage(Resident resident)
        {
            try
            {
                var isUserExist = await _residentRepository.AccountNumberExist(resident);
                if (!isUserExist)
                {
                    return BadRequest("Account number not found.");
                }

                //ADD VALIDATION IF ACCOUNT HAS ALREADY BEEN ACTIVATED
                var isActivated = await _residentRepository.IsAccountVerified(resident);

                if (isActivated)
                {
                    return BadRequest("Your account is already activated.");
                }

                //VALIDATION USERNAME EXIST
                var isUsernameExist = await _residentRepository.IsUsernameExist(resident.Username);
                if (isUsernameExist)
                {
                    return BadRequest("Username already exist.");
                }

                //VALIDATION EMAIL EXIST
                var isEmailExist = await _residentRepository.IsEmailExist(resident.Email);
                if (isEmailExist)
                {
                    return BadRequest("The email had already been used.");
                }

                var updateDetails = await _residentRepository.UpdateResidentDetails(resident);

                if (string.IsNullOrEmpty(updateDetails))
                {
                    return BadRequest("There was an error activating the account. Please try again later");
                }

                return Ok(new { message = "Proceed to next step verify the email", token = updateDetails });
                //if (Request.Cookies.ContainsKey("verifyToken"))
                //{
                //    // Retrieve the token from the cookie
                //    string token = Request.Cookies["verifyToken"] ?? string.Empty;
                //    string email = await _residentRepository.GetEmailByToken(token);
                //    if (!string.IsNullOrEmpty(token))
                //    {
                //        // Validate the token (e.g., check in the database)
                //        if (await _residentRepository.IsTokenExist(token))
                //        {
                //            if (!string.IsNullOrEmpty(email))
                //            {
                //                ViewData["Email"] = email;
                //                return View("~/Views/Resident/Signup/VerifyAccount.cshtml");
                //            }
                //        }
                //    }
                //}
                //return View("~/Views/Resident/Signup/VerifyAccount.cshtml");
                //return View("~/Views/Resident/RLogin/Index.cshtml");

                //return View("~/Views/Shared/Error.cshtml");
            }
            catch (Exception)
            {
                return View("~/Views/Resident/RLogin/Index.cshtml");
            }
        }

        public async Task<IActionResult> VerifyEmail(string? token)
        {
            try
            {
                // Check if token is provided in the URL; otherwise, fallback to cookie
                if (string.IsNullOrEmpty(token) && Request.Cookies.ContainsKey("verifyToken"))
                {
                    token = Request.Cookies["verifyToken"];
                }

                if (!string.IsNullOrEmpty(token))
                {
                    // Validate the token
                    if (await _residentRepository.IsTokenExist(token))
                    {
                        string email = await _residentRepository.GetEmailByToken(token);
                        if (!string.IsNullOrEmpty(email))
                        {
                            ViewData["Email"] = email;
                            return View("~/Views/Resident/Signup/VerifyAccount.cshtml");
                        }
                    }
                }

                // Token not valid or not found
                return View("~/Views/Shared/Error.cshtml");
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                Console.WriteLine($"Error during verification: {ex.Message}");
                return View("~/Views/Resident/RLogin/Index.cshtml");
            }
        }


        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Signup(Resident formData)
        //{
        //    try
        //    {
        //        if (formData == null || !ModelState.IsValid)
        //        {
        //            return BadRequest(new { message = 0 });
        //        }

        //        if (await _residentRepository.UserExists(formData))
        //        {
        //            return BadRequest("Email or Username already taken.");
        //        }


        //        string result = await _residentRepository.CreateResident(formData);

        //        if (string.IsNullOrEmpty(result))
        //            return BadRequest("There was an error creating the account. Please try again later.");

        //        //Response.Cookies.Append("verificationToken", result, new CookieOptions
        //        //{
        //        //    HttpOnly = false, // Allow JavaScript to access the cookie
        //        //    Secure = true, // Use only over HTTPS
        //        //    SameSite = SameSiteMode.Strict, // Strict same-site policy
        //        //    Expires = DateTime.Now.AddHours(1) // Cookie expires in 1 hour
        //        //});

        //        return Ok(new { message = "Registration Successful.", token = result });
        //        //return Ok("Registration Successful.");
        //    }
        //    catch (Exception)
        //    {
        //        return BadRequest("An error occurred while processing your request.");
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> VerifyCode(string Code, string Email)
        {
            int result = await _residentRepository.IsCodeTheSame(Code, Email);
            switch (result)
            {
                case 1:
                    await DeleteTokenCookie();
                    return Ok();
                case 0:
                    return BadRequest("Invalid code. Please try again.");
                default:
                    return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendCode(string Email)
        {
            int result = await _residentRepository.AddVerification(Email);
            switch (result)
            {
                case 0:
                    return BadRequest("There was an error sending the verification code. Please try again later.");
                case 1:
                    return Ok("Verification code sent successfully. Please check your email.");
                case 2:
                    return BadRequest("A verification code has already been sent and is still valid. Please check your email or wait before requesting a new code.");
                default:
                    return BadRequest("An unexpected error occurred. Please contact support.");
            }

        }

        public async Task<IActionResult> ForgotPassword()
        {

            return View("~/Views/Resident/RLogin/ForgotPassword.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string password, string email)
        {
            int result = await _residentRepository.UpdatePassword(password, email);
            if (result < 1)
            {
                return BadRequest("There was an error changing your password. Please try again later");
            }
            return Ok("The password changed successfully");
        }

        public async Task DeleteTokenCookie()
        {
            Response.Cookies.Delete("verifyToken");
        }


    }
}
