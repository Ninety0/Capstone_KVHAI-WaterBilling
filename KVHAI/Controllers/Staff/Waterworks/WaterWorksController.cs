using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace KVHAI.Controllers.Staff.Waterworks
{
    public class WaterWorksController : Controller
    {
        private readonly AddressRepository _addressRepository;
        private readonly WaterReadingRepository _waterReadingRepository;

        public WaterWorksController(AddressRepository addressRepository, WaterReadingRepository waterReadingRepository)
        {
            _addressRepository = addressRepository;
            _waterReadingRepository = waterReadingRepository;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                var _residentAddress = await _addressRepository.GetResidentAddressList();

                return View("~/Views/Staff/Waterworks/Index.cshtml", _residentAddress);
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
                int result = await _waterReadingRepository.CreateWaterReading(waterReading);
                if (result < 1)
                {
                    return BadRequest("There was an error processing the reading consumption");
                }
                return Ok("Submit successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetName(ResidentAddress residentAddress)
        {
            int index = 1;
            try
            {
                var resident = await _addressRepository.GetName(residentAddress);

                if (resident.Count < 1)
                {
                    return BadRequest("There is no resident in specified address");
                }
                index++;

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
