using KVHAI.Hubs;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace KVHAI.Controllers.Staff.Waterworks
{
    [Authorize(AuthenticationSchemes = "AdminCookieAuth", Roles = "waterworks")]

    public class WaterWorksController : Controller
    {
        private readonly AddressRepository _addressRepository;
        private readonly WaterReadingRepository _waterReadingRepository;
        private readonly StreetRepository _streetRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IHubContext<StaffNotificationHub> _staffhubContext;

        public WaterWorksController(AddressRepository addressRepository, WaterReadingRepository waterReadingRepository, 
        StreetRepository streetRepository, NotificationRepository notificationRepository, 
        IHubContext<NotificationHub> hubContext, IHubContext<StaffNotificationHub> staffhubContext)
        {
            _addressRepository = addressRepository;
            _waterReadingRepository = waterReadingRepository;
            _streetRepository = streetRepository;
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _staffhubContext = staffhubContext;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                var streets = await _streetRepository.GetAllStreets();

                return View("~/Views/Staff/Waterworks/Index.cshtml", streets);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubmitReading(WaterReading waterReading)
        {
            try
            {
                if (await _waterReadingRepository.CheckExistReading(waterReading.Address_ID))
                {
                    return BadRequest("There is already existing reading for the address specified.");
                }

                int result = await _waterReadingRepository.CreateWaterReading(waterReading);
                if (result < 1)
                {
                    return BadRequest("There was an error processing the reading consumption");
                }

                var notif = new Notification
                {
                    Resident_ID = waterReading.Resident_ID,
                    Title = "Water Reading",
                    Message = "You have new water reading",
                    Url = "/kvhai/resident/water-consumption",
                    Message_Type = "Personal"
                };
                var notificationResult = await _notificationRepository.InsertNotificationPersonal(notif);

                var notifStaff = new Notification
                {
                    Resident_ID = waterReading.Resident_ID,
                    Title = "Water Reading",
                    Message = "New water reading",
                    Url = "/kvhai/staff/water-reading/",
                    Message_Type = "clerk"
                };

                var notificationAdminResult = await _notificationRepository.SendNotificationAdminToAdmin(notif);
                await _staffhubContext.Clients.All.SendAsync("ReceivedWaterReading");


                return Ok("Submit successfully");
                //ViewData["Message"] = "Reading submit successfully!";
                //return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetName(ResidentAddress residentAddress)
        {
            try
            {
                var resident = await _addressRepository.GetName(residentAddress);

                if (resident.Count < 1)
                {
                    return BadRequest("There is no resident in specified address");
                }

                //return View("~/Views/Staff/Waterworks/Index.cshtml");
                return Json(resident);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
