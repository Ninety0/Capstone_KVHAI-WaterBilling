using KVHAI.CustomClass;
using KVHAI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;


namespace KVHAI.Controllers.Homeowner
{
    public class WaterConsumptionController : Controller
    {
        private readonly WaterBillingFunction _waterBillingFunction;
        private readonly AddressRepository _addressRepository;
        private readonly ResidentAddressRepository _residentAddressRepository;
        private readonly NotificationRepository _notification;
        private readonly ICompositeViewEngine _viewEngine;


        public WaterConsumptionController(WaterBillingFunction waterBillingFunction, AddressRepository addressRepository, ResidentAddressRepository residentAddressRepository, ICompositeViewEngine viewEngine, NotificationRepository notification)
        {
            _waterBillingFunction = waterBillingFunction;
            _addressRepository = addressRepository;
            _residentAddressRepository = residentAddressRepository;
            _viewEngine = viewEngine;
            _notification = notification;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            int addressID = 0;

            var address = new List<Models.Address>();

            if (role == "1")
            {
                address = await _addressRepository.GetAddressessByResId(residentID);
            }
            else
            {
                address = await _residentAddressRepository.GetAddressessByResId(residentID);
            }



            if (address.Count > 0)
            {
                await GetWaterConsumptionByAddress(address.FirstOrDefault().Address_ID.ToString(), DateTime.Now.ToString("yyyy"));
                addressID = address.Select(a => a.Address_ID).FirstOrDefault();
            }

            _waterBillingFunction.AddressList = address;
            //await _waterBillingFunction.GetGraphData(address.FirstOrDefault().Address_ID.ToString());
            _waterBillingFunction.NotificationResident = await _notification.GetNotificationByResident(residentID);

            return View("~/Views/Resident/LoggedIn/WaterConsumption.cshtml", _waterBillingFunction);
        }

        [HttpGet]
        public async Task<IActionResult> GraphWaterReading(string addressID, string year = "")

        {
            if (string.IsNullOrEmpty(addressID))
            {
                return BadRequest("You currently have no address.");
            }
            //var forecastData = await _waterBillingFunction.GetGraphDataReplicate(addressID, year);
            var forecastData = await _waterBillingFunction.GetGraphDataDatabase(addressID, year);
            //var forecastData = await _waterBillingFunction.GetGraphData(addressID, year);

            if (forecastData == null)
            {
                return BadRequest("There was an error loading the data.");
            }
            return Json(forecastData);
            //return Json(_waterBillingFunction.GraphData);
        }

        [HttpGet]
        public async Task<IActionResult> GraphWaterReadingOrig(string addressID, string year = "")

        {
            if (string.IsNullOrEmpty(addressID))
            {
                return BadRequest("You currently have no address.");
            }
            var forecastData = await _waterBillingFunction.GetGraphDataReplicate(addressID, year);
            //var forecastData = await _waterBillingFunction.GetGraphData(addressID, year);

            if (forecastData == null)
            {
                return BadRequest("There was an error loading the data.");
            }
            return Json(forecastData);
            //return Json(_waterBillingFunction.GraphData);
        }

        //[HttpPost]
        //public async Task<IActionResult> WaterReadingByAddress(string addressID)
        //{
        //    var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    var address = await _addressRepository.GetAddressById(residentID);

        //    await _waterBillingFunction.GetGraphData(addressID);
        //    await GetWaterConsumptionByAddress(addressID, residentID);

        //    var test = Json(_waterBillingFunction.GraphData);

        //    return View("~/Views/Resident/Reading/WaterConsumption.cshtml", _waterBillingFunction);
        //}

        [HttpGet]
        public async Task<IActionResult> WaterReadingByAddress(string addressID, string year = "")

        {
            var residentID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var forecastData = await _waterBillingFunction.GetGraphDataDatabase(addressID, year);

            //var forecastData = await _waterBillingFunction.GetGraphData(addressID, year);
            await GetWaterConsumptionByAddress(addressID, year);

            var partialView = await this.RenderViewAsync("~/Views/Resident/LoggedIn/WaterConsumption.cshtml", _waterBillingFunction, false);
            return Json(new
            {
                Html = partialView,
                GraphData = forecastData
            });

            //var viewName = "~/Views/Resident/Reading/WaterConsumption.cshtml";
            //var result = _viewEngine.FindView(ControllerContext, viewName, false);

            //if (result.Success)
            //{
            //    var partialView = await this.RenderViewAsync(viewName, _waterBillingFunction, false);
            //    return Json(new
            //    {
            //        Html = partialView,
            //        GraphData = _waterBillingFunction.GraphData
            //    });
            //}
            //else
            //{
            //    var searchedLocations = result.SearchedLocations.Aggregate((a, b) => a + ", " + b);
            //    return Content($"Error: The view '{viewName}' was not found. Searched locations: {searchedLocations}");
            //}
        }



        private async Task<string> RenderPartialViewToString(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.ActionDescriptor.ActionName;

            ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                ViewEngineResult viewResult =
                    _viewEngine.FindView(ControllerContext, viewName, false);

                if (viewResult.Success == false)
                {
                    return $"A view with the name {viewName} could not be found";
                }

                ViewContext viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);

                return writer.GetStringBuilder().ToString();
            }
        }


        private async Task GetWaterConsumptionByAddress(string addressID)
        {
            await _waterBillingFunction.WaterReading(addressID: addressID);

            var listConsumptions = new List<double>();
            var listCubic = new List<Double>();

            foreach (var item in _waterBillingFunction.AllWaterReadingByResident)
            {
                listConsumptions.Add(Convert.ToDouble(item.Consumption));
            }

            for (int i = listConsumptions.Count - 1; i > 0; i--)
            {
                var difference = listConsumptions[i - 1] - listConsumptions[i];
                listCubic.Add(difference);
            }

            _waterBillingFunction.CubicMeter = listCubic;
            _waterBillingFunction.CubicMeter.Reverse();

            var previous = _waterBillingFunction.PreviousReading;
            var current = _waterBillingFunction.CurrentReading;
            var address = _waterBillingFunction.ResidentAddress;

            var prevConsumption = previous.Where(reading => reading.Address_ID == addressID).ToList();
            var currentConsumption = current.Where(reading => reading.Address_ID == addressID).ToList();

            var addressList = address.Where(reading => reading.Address_ID.ToString() == addressID).ToList();

            _waterBillingFunction.PreviousReading = prevConsumption;
            _waterBillingFunction.CurrentReading = currentConsumption;
            _waterBillingFunction.ResidentAddress = addressList;


            for (int i = 0; i < addressList.Count; i++)
            {
                var cubic = 0.0;
                double previousConsumption = 0;
                double nextConsumption = 0;

                // Check if the index is within range for PreviousReading
                if (i < prevConsumption.Count && !double.TryParse(prevConsumption[i].Consumption, out previousConsumption))
                {
                    previousConsumption = 0; // Default value if parsing fails
                }

                // Check if the index is within range for CurrentReading
                if (i < currentConsumption.Count && !double.TryParse(currentConsumption[i].Consumption, out nextConsumption))
                {
                    nextConsumption = 0; // Default value if parsing fails
                }

                cubic = (nextConsumption - previousConsumption) < 1 ? 0 : nextConsumption - previousConsumption;

                _waterBillingFunction.CubicMeter.Add(cubic);

                _waterBillingFunction.CubicConsumption = cubic.ToString();
            }

        }

        private async Task GetWaterConsumptionByAddress(string addressID, string year)
        {
            await _waterBillingFunction.WaterReading(addressID: addressID, _year: year);

            var listConsumptions = new List<double>();
            var listCubic = new List<double>();

            // Filter AllWaterReadingByResident by year if a year is specified
            var allReadingsByYear = string.IsNullOrEmpty(year)
                ? _waterBillingFunction.AllWaterReadingByResident
                : _waterBillingFunction.AllWaterReadingByResident
                    .Where(item => DateTime.Parse(item.Date).Year.ToString() == year).ToList();

            foreach (var item in allReadingsByYear)
            {
                listConsumptions.Add(Convert.ToDouble(item.Consumption));
            }

            for (int i = listConsumptions.Count - 1; i > 0; i--)
            {
                var difference = listConsumptions[i - 1] - listConsumptions[i];
                listCubic.Add(difference);
            }

            _waterBillingFunction.CubicMeter = listCubic;
            _waterBillingFunction.CubicMeter.Reverse();

            var previous = _waterBillingFunction.PreviousReading;
            var current = _waterBillingFunction.CurrentReading;
            var address = _waterBillingFunction.ResidentAddress;

            // Filter by addressID and year
            var prevConsumption = previous
                .Where(reading => reading.Address_ID == addressID && DateTime.Parse(reading.Date).Year.ToString() == year)
                .ToList();

            var currentConsumption = current
                .Where(reading => reading.Address_ID == addressID && DateTime.Parse(reading.Date).Year.ToString() == year)
                .ToList();

            var addressList = address
                .Where(reading => reading.Address_ID.ToString() == addressID)
                .ToList();

            _waterBillingFunction.PreviousReading = prevConsumption;
            _waterBillingFunction.CurrentReading = currentConsumption;
            _waterBillingFunction.ResidentAddress = addressList;

            for (int i = 0; i < addressList.Count; i++)
            {
                var cubic = 0.0;
                double previousConsumption = 0;
                double nextConsumption = 0;

                // Check if the index is within range for PreviousReading
                if (i < prevConsumption.Count && !double.TryParse(prevConsumption[i].Consumption, out previousConsumption))
                {
                    previousConsumption = 0; // Default value if parsing fails
                }

                // Check if the index is within range for CurrentReading
                if (i < currentConsumption.Count && !double.TryParse(currentConsumption[i].Consumption, out nextConsumption))
                {
                    nextConsumption = 0; // Default value if parsing fails
                }

                cubic = (nextConsumption - previousConsumption) < 1 ? 0 : nextConsumption - previousConsumption;

                _waterBillingFunction.CubicMeter.Add(cubic);

                _waterBillingFunction.CubicConsumption = cubic.ToString();
            }
        }

    }

    public static class RenderViewToStringHelper
    {
        //public static async Task<string> RenderViewAsync<TModel>(this Controller controller, string viewName, TModel model, bool isPartial = false)
        //{
        //    // If no viewName is specified, use the action name as the view name.
        //    if (string.IsNullOrEmpty(viewName))
        //    {
        //        viewName = controller.ControllerContext.ActionDescriptor.ActionName;
        //    }

        //    controller.ViewData.Model = model;

        //    using (var sw = new StringWriter())
        //    {
        //        var viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
        //        var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, !isPartial);

        //        if (!viewResult.Success)
        //        {
        //            var searchedLocations = string.Join(", ", viewResult.SearchedLocations);
        //            return $"View {viewName} not found. Searched locations: {searchedLocations}";
        //        }

        //        var viewContext = new ViewContext(
        //            controller.ControllerContext,
        //            viewResult.View,
        //            controller.ViewData,
        //            controller.TempData,
        //            sw,
        //            new HtmlHelperOptions()
        //        );

        //        await viewResult.View.RenderAsync(viewContext);
        //        return sw.GetStringBuilder().ToString();
        //    }
        //}

        public static async Task<string> RenderViewAsync<TModel>(this Controller controller, string viewName, TModel model, bool isPartial = false)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;
            }

            controller.ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;

                ViewEngineResult viewResult;
                if (viewName.StartsWith("~/"))
                {
                    // If the view name starts with "~/", treat it as an absolute path
                    viewResult = viewEngine.GetView("~/", viewName, !isPartial);
                }
                else
                {
                    // Otherwise, use the default view location logic
                    viewResult = viewEngine.FindView(controller.ControllerContext, viewName, !isPartial);
                }

                if (!viewResult.Success)
                {
                    var searchedLocations = string.Join(", ", viewResult.SearchedLocations);
                    return $"View {viewName} not found. Searched locations: {searchedLocations}";
                }

                var viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.GetStringBuilder().ToString();
            }
        }

    }
}
