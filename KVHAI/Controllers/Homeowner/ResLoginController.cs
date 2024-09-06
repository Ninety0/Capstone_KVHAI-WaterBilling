using KVHAI.CustomClass;
using KVHAI.Interface;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Homeowner

{
    public class ResLoginController : Controller
    {
        private readonly ResidentRepository _residentRepository;
        private readonly StreetRepository _streetRepository;
        private readonly ImageUploadRepository _imageRepository;
        private readonly InputSanitize _sanitize;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailSender _emailService;


        public ResLoginController(ResidentRepository residentRepository, StreetRepository streetRepository, InputSanitize sanitize, ImageUploadRepository imageUpload, IWebHostEnvironment environment, IEmailSender emailService)
        {
            _residentRepository = residentRepository;
            _streetRepository = streetRepository;
            _sanitize = sanitize;
            _imageRepository = imageUpload;
            _environment = environment;
            _emailService = emailService;
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

        public async Task<IActionResult> VerifyPage(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return View("~/Views/Resident/RLogin/Index.cshtml");
                }

                ViewData["token"] = token;

                return View("~/Views/Resident/Signup/VerifyAccount.cshtml");
            }
            catch (Exception)
            {
                return View("~/Views/Resident/RLogin/Index.cshtml");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(Resident formData)
        {
            try
            {
                if (formData == null || !ModelState.IsValid)
                {
                    return BadRequest(new { message = 0 });
                }

                if (await _residentRepository.UserExists(formData))
                {
                    return BadRequest("Email or Username already taken.");
                }


                //string result = await _residentRepository.CreateResident(formData);
                string result = "token";

                if (string.IsNullOrEmpty(result))
                    return BadRequest("There was an error saving the resident and the image.");

                //Response.Cookies.Append("verificationToken", result, new CookieOptions
                //{
                //    HttpOnly = false, // Allow JavaScript to access the cookie
                //    Secure = true, // Use only over HTTPS
                //    SameSite = SameSiteMode.Strict, // Strict same-site policy
                //    Expires = DateTime.Now.AddHours(1) // Cookie expires in 1 hour
                //});

                return Ok(new { message = "Registration Successful.", token = result });
                //return Ok("Registration Successful.");
            }
            catch (Exception)
            {
                return BadRequest("An error occurred while processing your request.");
            }
        }

        [HttpPost]
        public IActionResult SendEmail(EmailDto request)
        {
            request.To = "dorojavince@gmail.com";
            request.Subject = "Verification";
            request.Body = "1234";
            _emailService.SendEmail(request);
            return Ok();
        }

    }
}
