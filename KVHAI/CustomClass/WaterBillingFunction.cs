using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Html;

namespace KVHAI.CustomClass
{
    public class WaterBillingFunction
    {
        private readonly WaterReadingRepository _waterReadingRepository;
        private readonly AddressRepository _addressRepository;

        private const double WaterRate = 18.0;

        public string CoverageDateFrom { get; set; } = string.Empty;

        public string CoverageDateTo { get; set; } = string.Empty;


        public List<WaterReading>? PreviousReading { get; set; }
        public List<WaterReading>? CurrentReading { get; set; }
        public List<ResidentAddress>? ResidentAddress { get; set; }

        public List<string>? WaterBillNumbers { get; set; }
        public List<string>? WaterReadingNumbers { get; set; } = new List<string>();
        public List<string>? ReadingStartDateRange { get; set; } = new List<string>();
        public List<string>? ReadingEndDateRange { get; set; } = new List<string>();
        public List<Double> CubicMeter { get; set; }
        public List<Double> BillAmount { get; set; }

        public int CountData { get; set; }
        public string ClassActive { get; set; }

        public string CurrentFirstDate = string.Empty;
        public string CurrentMonth = string.Empty;
        public string CurrentLastDate = string.Empty;

        public string PrevFirstDate = string.Empty;
        public string PrevMonth = string.Empty;
        public string PrevLastDate = string.Empty;

        public string MonthlyBillString = string.Empty;
        public string WaterBill_No { get; set; } = string.Empty;

        public string ErrorMessage = string.Empty;

        public string Location { get; set; } = string.Empty;

        public HtmlString GenerateButton = new HtmlString("");
        public HtmlString GenerateSelect = new HtmlString("");

        int index = 1;
        public WaterBillingFunction(WaterReadingRepository waterReadingRepository, AddressRepository addressRepository)
        {
            _waterReadingRepository = waterReadingRepository;
            _addressRepository = addressRepository;
            CubicMeter = new List<double>();
            BillAmount = new List<Double>();
        }

        //public async Task UseWaterBillingByBatch()
        //{
        //    var prevReading = await _waterReadingRepository.GetPreviousReading(location);
        //    var currentReading = await _waterReadingRepository.GetCurrentReading(location);
        //}

        public async Task UseWaterBilling(string location = "", string dateFrom = "", string dateTo = "")
        {
            var prevReading = await _waterReadingRepository.GetPreviousReading(location, dateFrom);
            var currentReading = await _waterReadingRepository.GetCurrentReading(location, dateTo);

            this.WaterBillNumbers = await _waterReadingRepository.GetWaterBillNo();
            (this.ReadingStartDateRange, this.ReadingEndDateRange) = await _waterReadingRepository.WaterReadingList();


            var model = new ModelBinding
            {
                PreviousReading = prevReading.PreviousReading,
                CurrentReading = currentReading.CurrentReading,
                ResidentAddress = prevReading.ResidentAddress

            };

            this.PreviousReading = model.PreviousReading;
            this.CurrentReading = model.CurrentReading;
            this.ResidentAddress = model.ResidentAddress;

            // PARSING DATES
            this.CurrentFirstDate = CurrentReading?.Count < 1 ? string.Empty : ParseStartDate(CurrentReading?[0].Date);
            this.CurrentLastDate = CurrentReading?.Count < 1 ? string.Empty : ParseLastDate(CurrentReading?[GetLastIndex(CurrentReading)].Date);
            this.CurrentMonth = CurrentReading?.Count < 1 ? string.Empty : "-" + ParseMonth(CurrentReading?[0].Date) + "-";


            this.PrevFirstDate = PreviousReading?.Count < 1 ? string.Empty : ParseStartDate(PreviousReading?[0].Date);//PreviousReading?[0].Date.ToString() ?? string.Empty;
            this.PrevLastDate = PreviousReading?.Count < 1 ? string.Empty : ParseLastDate(PreviousReading?[GetLastIndex(PreviousReading)].Date);
            this.PrevMonth = PreviousReading?.Count < 1 ? string.Empty : "-" + ParseMonth(PreviousReading?[0].Date) + "-";

            this.MonthlyBillString = DateTime.Now.AddMonths(-1).ToString("MMM");

            this.CountData = await _addressRepository.GetCountByLocation(location);

            this.ClassActive = CurrentReading?.Count == CountData ? "active" : "disabled";

            this.GenerateButton = await Button(location);
            this.GenerateSelect = await WaterReadingSelect();
            this.Location = location;

            try
            {
                for (int i = 0; i < ResidentAddress.Count; i++)
                {
                    var cubic = 0.0;
                    var amount = 0.0;
                    double previousConsumption = 0;
                    double currentConsumption = 0;

                    // Check if the index is within range for PreviousReading
                    if (i < PreviousReading.Count && !double.TryParse(PreviousReading[i].Consumption, out previousConsumption))
                    {
                        previousConsumption = 0; // Default value if parsing fails
                    }

                    // Check if the index is within range for CurrentReading
                    if (i < CurrentReading.Count && !double.TryParse(CurrentReading[i].Consumption, out currentConsumption))
                    {
                        currentConsumption = 0; // Default value if parsing fails
                    }

                    // Calculate cubic difference
                    cubic = (currentConsumption - previousConsumption) < 1 ? 0 : currentConsumption - previousConsumption;

                    // Calculate bill amount
                    amount = cubic * WaterRate;

                    // Add the computed values to the lists
                    this.CubicMeter.Add(cubic);
                    this.BillAmount.Add(amount);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }


        }

        private async Task<HtmlString> Button(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                return new HtmlString("");
            }

            string button = "<button id=\"btnGenerate \" class=\"mt-3 mb-3 p-2 btn btn-primary " + ClassActive + " \"> Generate Bill </button>";

            return new HtmlString(button);
        }

        private async Task<HtmlString> WaterReadingSelect()
        {
            if (ReadingStartDateRange == null && ReadingEndDateRange == null)
            {
                return new HtmlString("");
            }
            var _outStartDate = "";
            var _outEndDate = "";
            string select = "<select class=\"form-select w-75\" id=\"waterReadingSelect\" aria-label=\"Water Reading Number\">";
            for (int i = 0; i < ReadingStartDateRange.Count; i++)
            {

                if (DateTime.TryParse(ReadingStartDateRange[i], out DateTime startDate))
                {
                    _outStartDate = startDate.ToString("MMMM");
                }
                if (DateTime.TryParse(ReadingEndDateRange[i], out DateTime endDate))
                {
                    _outEndDate = endDate.ToString("MMMM");
                }

                var text = $"Water Reading {_outStartDate} To {_outEndDate}";
                var selected = i == 0 ? "selected" : "";
                select += $"<option data-dateFrom=\" {ReadingStartDateRange[i]} \" data-dateTo=\" {ReadingEndDateRange[i]} \" value=\"{i}\" {selected}>{text}</option>";
            }
            select += "</select>";

            CoverageDateFrom = _outStartDate;
            CoverageDateTo = _outEndDate;

            return new HtmlString(select);
        }

        private string ParseStartDate(string? _startDate)
        {
            if (_startDate == null)
            {

            }
            string? _outStartDate = "";

            if (DateTime.TryParse(_startDate, out DateTime startDate))
            {
                _outStartDate = startDate.ToString("dd");
            }

            return _outStartDate;
        }

        private string ParseLastDate(string? _lastDate)
        {
            string? _outLastDate = "";

            if (DateTime.TryParse(_lastDate, out DateTime startDate))
            {
                _outLastDate = startDate.ToString("dd");
            }

            return _outLastDate;
        }

        private string ParseMonth(string? _month)
        {
            string? _outMonth = "";

            if (DateTime.TryParse(_month, out DateTime startDate))
            {
                _outMonth = startDate.ToString("MMM");
            }
            return _outMonth;
        }

        public int GetLastIndex(List<WaterReading> _date)
        {
            int index = 0;

            for (int i = _date.Count - 1; i >= 0; i--)
            {
                var date = _date[i].Date;
                if (!string.IsNullOrEmpty(date))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
    }
}

/*
 
 try
            {
                for (int i = 0; i < PreviousReading?.Count; i++)
                {
                    var cubic = 0.0;
                    var amount = 0.0;
                    double previousConsumption = 0;
                    double currentConsumption = 0;

                    // Attempt to parse previous consumption
                    if (!double.TryParse(PreviousReading[i].Consumption, out previousConsumption))
                    {
                        previousConsumption = 0; // Default value if parsing fails
                    }

                    // Attempt to parse current consumption
                    //if (!double.TryParse(CurrentReading?[i].Consumption, out currentConsumption))
                    //{
                    //    currentConsumption = 0; // Default value if parsing fails
                    //}

                    // Calculate cubic difference
                    //cubic = currentConsumption - previousConsumption;

                    ////Calculate bill amount
                    //amount = cubic * WaterRate;

                    //this.CubicMeter.Add(cubic);
                    //this.BillAmount.Add(amount);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }
 */