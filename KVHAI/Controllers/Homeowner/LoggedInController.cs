﻿using KVHAI.Hubs;
using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Security.Claims;

namespace KVHAI.Controllers.Homeowner
{
    public class LoggedInController : Controller
    {
        private readonly StreetRepository _streetRepository;
        private readonly AddressRepository _addressRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly AnnouncementRepository _announcementRepository;
        private readonly IHubContext<AnnouncementHub> _hubContext;
        private readonly NotificationRepository _notification;



        public LoggedInController(StreetRepository streetRepository, AddressRepository addressRepository, IWebHostEnvironment environment, AnnouncementRepository announcementRepository, IHubContext<AnnouncementHub> hubContext, NotificationRepository notification)
        {
            _streetRepository = streetRepository;
            _addressRepository = addressRepository;
            _environment = environment;
            _announcementRepository = announcementRepository;
            _hubContext = hubContext;
            _notification = notification;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var address_id = await _addressRepository.GetAddressIDByResidentID(residentID, role);

            var announcments = await _announcementRepository.ShowAnnouncement();
            var notifList = await _notification.GetNotificationByResident(residentID);


            var viewModel = new ModelBinding
            {
                Resident_ID = residentID,
                Username = username,
                Role = role,
                AnnouncementList = announcments,
                NotificationResident = notifList
            };
            //await _hubContext.Clients.All.SendAsync("ShowAnnouncement");

            return View("~/Views/Resident/LoggedIn/Announcement.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAnnouncement()
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var model = await _announcementRepository.ShowAnnouncement();
            var notifList = await _notification.GetNotificationByResident(residentID);


            var viewModel = new ModelBinding
            {
                AnnouncementList = model,
                NotificationResident = notifList
            };

            return View("~/Views/Resident/LoggedIn/Announcement.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetNewNotification(string resident_id)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var model = await _announcementRepository.ShowAnnouncement();

            var address_id = await _addressRepository.GetAddressIDByResidentID(resident_id, role);

            var announcments = await _announcementRepository.ShowAnnouncement();
            var notifList = await _notification.GetNotificationByResident(resident_id);

            var viewModel = new ModelBinding
            {
                NotificationResident = notifList,
                AnnouncementList = model
            };

            return View("~/Views/Resident/LoggedIn/Announcement.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNotificationRead(string notification_id)
        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int result = await _notification.UpdateReadNotification(notification_id, residentID);
            return Ok();
        }

        [Authorize]
        public async Task<IActionResult> LoggedIn()
        {
            var username = User.Identity.Name;
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var notifList = await _notification.GetNotificationByResident(residentID);
            var listStreet = await _streetRepository.GetAllStreets();

            var viewModel = new ModelBinding
            {
                NotificationResident = notifList,
                ListStreet = listStreet
            };

            if (role == "1")//owner
            {
                return View("~/Views/Resident/LoggedIn/Owner/RegisterAddress.cshtml", viewModel);
            }
            else //renter
            {
                return View("~/Views/Resident/LoggedIn/Renter/RenterRegisterAddress.cshtml", viewModel);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            //Response.Redirect("/kvhai/resident/login");
            return Ok("Account will be logged out");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string addresses, List<IFormFile> files)
        {
            try
            {
                var resID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var addressData = JsonConvert.DeserializeObject(addresses);
                var addressDataList = JsonConvert.DeserializeObject<List<Address>>(addresses);
                var modelAddressList = new List<Address>();

                foreach (var item in addressDataList)
                {
                    // Check if there is already an entry with the same lot and street name in the modelAddressList
                    bool isDuplicate = modelAddressList.Any(a => a.Lot == item.Lot && a.Street_Name == item.Street_Name);

                    // If it's not a duplicate, process the address and its associated file
                    if (!isDuplicate)
                    {
                        var addressModel = new Address
                        {
                            Block = item.Block,
                            Lot = item.Lot,
                            Street_Name = item.Street_Name
                        };
                        modelAddressList.Add(addressModel);
                    }
                }

                int result = await _addressRepository.CreateAddressandUploadImage(resID, modelAddressList, files, _environment.WebRootPath);

                if (result == 0)
                    return BadRequest("There was an error saving the resident and the image.");

                if (result == 2)
                {
                    return Ok("Address already registered.");
                }

                return Ok("Registration Successful.");
            }
            catch (Exception)
            {
                return BadRequest("An error occurred while processing your request.");
            }
        }
    }
}
