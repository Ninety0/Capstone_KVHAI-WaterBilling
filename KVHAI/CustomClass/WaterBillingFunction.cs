using KVHAI.Models;
using KVHAI.Repository;
using Microsoft.AspNetCore.Html;
using System.Data;

namespace KVHAI.CustomClass
{
    public class WaterBillingFunction
    {
        private readonly WaterReadingRepository _waterReadingRepository;
        private readonly WaterBillRepository _waterBillRepository;
        private readonly AddressRepository _addressRepository;

        //FOR BOTH READINg AND BILLING
        public double WaterRate { get; set; } = 18.0;

        public List<WaterBilling>? UnpaidWaterBill { get; set; } = new List<WaterBilling>();
        public List<WaterReading>? PreviousReading { get; set; }
        public List<WaterReading>? CurrentReading { get; set; }
        public List<ResidentAddress>? ResidentAddress { get; set; }
        public List<WaterBilling>? WaterBill { get; set; }

        public List<Double> CubicMeter { get; set; }
        public List<Double> BillAmount { get; set; } = new List<double>();
        public List<Double> PreviousBillAmount { get; set; } = new List<double>();
        public List<Double> Total { get; set; } = new List<double>();

        public string ErrorMessage = string.Empty;
        public string Location { get; set; } = string.Empty;

        //FOR WATER READING
        public string WRCoverageDateFrom { get; set; } = string.Empty;
        public string WRCoverageDateTo { get; set; } = string.Empty;

        public string WRCurrentFirstDate = string.Empty;
        public string WRCurrentMonth = string.Empty;
        public string WRCurrentLastDate = string.Empty;

        public string WRPrevFirstDate = string.Empty;
        public string WRPrevMonth = string.Empty;
        public string WRPrevLastDate = string.Empty;

        public string MonthlyBillText = string.Empty;
        public int CountData { get; set; }
        public string ClassActive { get; set; }
        public List<string>? WaterReadingNumbers { get; set; } = new List<string>();

        public List<string>? ReadingStartDateRange { get; set; } = new List<string>();
        public List<string>? ReadingEndDateRange { get; set; } = new List<string>();
        //END WATER REAdING



        //FOR WATER BILLING
        public List<string>? WBDateTextList { get; set; } = new List<string>();
        public List<string>? WBArrearsBill { get; set; } = new List<string>();
        public List<string>? WaterBillNumberList { get; set; }
        public List<string>? WaterBillNumbers { get; set; }
        public List<string>? DateUssuedFrom { get; set; }
        public List<string>? DateUssuedTo { get; set; }
        public List<string>? DueDateFrom { get; set; }
        public List<string>? DueDateTo { get; set; }
        //END WATER BILLING



        //HTML ELEMENT
        public HtmlString GenerateButton = new HtmlString("");
        public HtmlString GenerateSelect = new HtmlString("");

        int index = 1;
        public WaterBillingFunction(WaterReadingRepository waterReadingRepository, WaterBillRepository waterBillRepository, AddressRepository addressRepository)
        {
            _waterReadingRepository = waterReadingRepository;
            _waterBillRepository = waterBillRepository;
            _addressRepository = addressRepository;
            CubicMeter = new List<double>();
            BillAmount = new List<Double>();
        }

        //public async Task UseWaterBillingByBatch()
        //{
        //    var prevReading = await _waterReadingRepository.GetPreviousReading(location);
        //    var currentReading = await _waterReadingRepository.GetCurrentReading(location);
        //}

        public async Task UseWaterBilling(string location = "", string dateFrom = "", string dateTo = "", string wbnumber = "")
        {
            //var prevReading = await _waterReadingRepository.GetPreviousReading(location, dateFrom);
            //var currentReading = await _waterReadingRepository.GetCurrentReading(location, dateTo);
            var prevReading = await _waterBillRepository.GetPreviousReading(location, dateFrom);
            var currentReading = await _waterBillRepository.GetCurrentReading(location, dateTo, wbnumber);

            this.WaterBillNumbers = await _waterReadingRepository.GetWaterBillNo();
            (this.ReadingStartDateRange, this.ReadingEndDateRange) = await _waterReadingRepository.WaterReadingList();
            this.WaterBillNumberList = await _waterBillRepository.WaterBillNumberList();

            var model = new ModelBinding
            {
                PreviousReading = prevReading.PreviousReading,
                CurrentReading = currentReading.CurrentReading,
                ResidentAddress = prevReading.ResidentAddress,
                WBilling = currentReading.WBilling
            };
            this.PreviousReading = model.PreviousReading;
            this.CurrentReading = model.CurrentReading;
            this.ResidentAddress = model.ResidentAddress;
            this.WaterBill = model.WBilling;

            this.UnpaidWaterBill = await _waterBillRepository.GetUnpaidWaterBill(PreviousReading);//GET ARREARS



            // PARSING DATES WATER READING
            this.WRCurrentFirstDate = CurrentReading?.Count < 1 ? string.Empty : ParseStartDate(CurrentReading?[0].Date);
            this.WRCurrentLastDate = CurrentReading?.Count < 1 ? string.Empty : ParseLastDate(CurrentReading?[GetLastIndex(CurrentReading)].Date);
            this.WRCurrentMonth = CurrentReading?.Count < 1 ? string.Empty : "-" + ParseMonth(CurrentReading?[0].Date) + "-";

            this.WRPrevFirstDate = PreviousReading?.Count < 1 ? string.Empty : ParseStartDate(PreviousReading?[0].Date);//PreviousReading?[0].Date.ToString() ?? string.Empty;
            this.WRPrevLastDate = PreviousReading?.Count < 1 ? string.Empty : ParseLastDate(PreviousReading?[GetLastIndex(PreviousReading)].Date);
            this.WRPrevMonth = PreviousReading?.Count < 1 ? string.Empty : "-" + ParseMonth(PreviousReading?[0].Date) + "-";
            //END PARSING


            this.MonthlyBillText = DateTime.Now.AddMonths(-1).ToString("MMM");

            this.CountData = await _addressRepository.GetCountByLocation(location);

            this.ClassActive = CurrentReading?.Count == CountData ? "active" : "disabled";

            this.GenerateButton = await Button(location);
            this.GenerateSelect = await WaterReadingSelect();
            this.Location = location;

            foreach (var item in model.PreviousReading)
            {
                var date = "";
                if (DateTime.TryParse(item.Date, out DateTime result))
                {
                    date = result.ToString("MMM yyyy");
                }
                WBDateTextList.Add(date);
            }

            try
            {
                for (int i = 0; i < ResidentAddress.Count; i++)
                {
                    var cubic = 0.0;
                    var amount = 0.0;
                    double previousConsumption = 0;
                    double currentConsumption = 0;
                    double previousWaterBillAmount = 0;
                    double TotalAmount = 0.0;

                    // Check if the index is within range for PreviousReading
                    if (i < UnpaidWaterBill.Count && !double.TryParse(UnpaidWaterBill[i].PreviousWaterBillAmount, out previousWaterBillAmount))
                    {
                        previousWaterBillAmount = 0; // Default value if parsing fails
                    }

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

                    //Calculate Total Amount
                    TotalAmount = amount + previousWaterBillAmount;

                    // Add the computed values to the lists
                    this.CubicMeter.Add(cubic);
                    this.BillAmount.Add(amount);
                    this.PreviousBillAmount.Add(previousWaterBillAmount);
                    this.Total.Add(TotalAmount);
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
                this.WRCoverageDateFrom = _outStartDate;
                this.WRCoverageDateTo = _outEndDate;

                var selected = i == 0 ? "selected" : "";
                select += $"<option data-dateFrom=\" {ReadingStartDateRange[i]} \" data-dateTo=\" {ReadingEndDateRange[i]} \" value=\"{_outStartDate}-{_outEndDate}\" {selected}>{text}</option>";
            }
            select += "</select>";


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

        public async Task<DataTable> WaterBillDataSet()
        {
            var dt = new DataTable();
            try
            {
                for (int i = 0; i < ResidentAddress.Count; i++)
                {
                    // Define default values for cases where data might be missing
                    string previousReading = "N/A";
                    string currentReading = "N/A";
                    string cubicMeter = "N/A";
                    string billAmount = "N/A";
                    string arrears = "N/A";
                    string waterBillNumber = "N/A";



                    // Fetch the previous reading if available
                    if (i < PreviousReading.Count)
                    {
                        previousReading = PreviousReading[i]?.Consumption ?? "N/A";
                    }

                    // Fetch the current reading if available
                    if (i < CurrentReading.Count)
                    {
                        currentReading = CurrentReading[i]?.Consumption ?? "N/A";
                    }

                    // Fetch the calculated cubic meter if available
                    if (i < CubicMeter.Count)
                    {
                        cubicMeter = CubicMeter[i].ToString();
                    }

                    // Fetch the calculated bill amount if available
                    if (i < BillAmount.Count)
                    {
                        billAmount = BillAmount[i].ToString("F2");
                    }

                }
            }
            catch (Exception ex)
            {

            }

            return dt;
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