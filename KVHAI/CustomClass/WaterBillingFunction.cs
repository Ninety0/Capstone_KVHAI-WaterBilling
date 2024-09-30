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
        public List<WaterReading>? PreviousReading { get; set; } = new List<WaterReading>();
        public List<WaterReading>? CurrentReading { get; set; } = new List<WaterReading>();
        public List<ResidentAddress>? ResidentAddress { get; set; } = new List<ResidentAddress>();
        public List<WaterBilling>? WaterBill { get; set; }

        public List<Double> CubicMeter { get; set; } = new List<double>();
        public List<Double> BillAmount { get; set; } = new List<double>();
        public List<Double> PreviousBillAmount { get; set; } = new List<double>();
        public List<Double> Total { get; set; } = new List<double>();

        public string ErrorMessage = string.Empty;
        public string Location { get; set; } = string.Empty;

        //FOR WATER READING
        public List<WaterReading>? AllWaterReadingByResident { get; set; } = new List<WaterReading>();
        public List<Address>? AddressList { get; set; } = new List<Address>();
        public List<WaterReading>? GraphData { get; set; } = new List<WaterReading>();
        public List<string>? GraphYear { get; set; } = new List<string>();
        public string CubicConsumption { get; set; } = string.Empty;
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
        public string WaterBillNumber { get; set; } = string.Empty;
        public List<string>? DateUssuedFrom { get; set; }
        public List<string>? DateUssuedTo { get; set; }
        public List<string>? DueDateFrom { get; set; }
        public List<string>? DueDateTo { get; set; }
        public List<string>? DueDate { get; set; } = new List<string>();
        public List<string>? DateIssued { get; set; } = new List<string>();
        public List<HTMLValueForWaterBilling>? WaterBillingValues { get; set; } = new List<HTMLValueForWaterBilling>();
        //END WATER BILLING

        //FOR UNPAID WATERBILL OF RESIDENT
        public List<string>? DueDateLong { get; set; } = new List<string>();
        public List<string>? DueDateShort { get; set; } = new List<string>();
        public List<string>? DueDateMonthShort { get; set; } = new List<string>();
        public List<string>? UnpaidMonthsLong { get; set; } = new List<string>();
        public List<string>? UnpaidMonthShort { get; set; } = new List<string>();
        public string UnpaidAmount { get; set; } = string.Empty;
        public List<Address>? UnpaidAddress { get; set; } = new List<Address>();

        //END UNPAID

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

        //FOR WATER READING
        public async Task WaterReading(string location = "", string dateFrom = "", string dateTo = "", string addressID = "", string residentID = "")
        {
            var prevReading = await _waterReadingRepository.GetPreviousReading(location, dateFrom);
            var currentReading = await _waterReadingRepository.GetCurrentReading(location, dateTo);

            if (!string.IsNullOrEmpty(addressID) && !string.IsNullOrEmpty(residentID))
            {
                var allReadingResident = await _waterReadingRepository.GetAllReadingByResident(addressID, residentID);

                var address = await _addressRepository.GetAddressById(residentID);

                this.AllWaterReadingByResident = allReadingResident.AllWaterConsumptionByResident;
                this.AddressList = address;

            }

            (this.ReadingStartDateRange, this.ReadingEndDateRange) = await _waterReadingRepository.WaterReadingList();
            this.MonthlyBillText = DateTime.Now.AddMonths(-1).ToString("MMM");


            //this.PreviousReading = prevReading.PreviousReading;
            //this.CurrentReading = currentReading.CurrentReading;
            //this.ResidentAddress = prevReading.ResidentAddress;

            await MapToModel(prevReading, currentReading);

            // PARSING DATES WATER READING
            await ParseWaterReadingDates();

            //this.CountData = await _addressRepository.GetCountByLocationWithReading(location);
            this.ClassActive = (PreviousReading?.Count > 0 || CurrentReading?.Count > 0) &&
                                (CurrentReading?.Count == PreviousReading?.Count) ? "active" : "disabled";
            //(CurrentReading?.Count == PreviousReading?.Count)

            this.GenerateButton = await Button(location);
            this.GenerateSelect = await WaterReadingSelect();

            try
            {
                await BillCalculations();
                await ReturnWaterBillingValues();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }

        }

        public async Task GetGraphData(string addressID, string year = "") //List<WaterReading>
        {
            var readings = await _waterReadingRepository.GerReadingForGraph(addressID, year);
            // Sort readings by date
            var sortedReadings = readings.OrderBy(r => r.Date).ToList();

            var graphData = new List<WaterReading>();

            var yearList = new List<string>();

            for (int i = 1; i < sortedReadings.Count; i++)
            {
                var currentReading = sortedReadings[i];
                var previousReading = sortedReadings[i - 1];
                string month = "";
                string _year = "";

                var consumptionDifference = Convert.ToInt32(currentReading.Consumption) - Convert.ToInt32(previousReading.Consumption);

                if (DateTime.TryParse(previousReading.Date, out DateTime Month))
                {
                    month = Month.ToString("MMMM");
                    _year = Month.ToString("yyyy");
                }

                var yearExist = yearList.Contains(_year);

                if (!yearExist)
                {
                    yearList.Add(_year);
                }

                graphData.Add(new WaterReading
                {
                    DateMonth = month,
                    CubicConsumption = consumptionDifference.ToString()
                });
            }
            this.GraphYear = yearList;
            this.GraphData = graphData;
        }

        //FOR WATER BILLING
        public async Task WaterBilling(string location = "", string dateFrom = "", string dateTo = "", string wbnumber = "")
        {
            var prevReading = await _waterBillRepository.GetPreviousReading(location, dateFrom);
            var currentReading = await _waterBillRepository.GetCurrentReading(location, dateTo, wbnumber);

            //this.WaterBillNumbers = await _waterReadingRepository.GetWaterBillNo();
            (this.ReadingStartDateRange, this.ReadingEndDateRange) = await _waterReadingRepository.WaterReadingList();
            this.WaterBillNumberList = await _waterBillRepository.WaterBillNumberList();

            await MapToModel(prevReading, currentReading);

            // PARSING DATES WATER READING
            await ParseWaterReadingDates();

            this.UnpaidWaterBill = await _waterBillRepository.GetUnpaidWaterBill(CurrentReading);//GET ARREARS

            this.MonthlyBillText = DateTime.Now.AddMonths(-1).ToString("MMM");

            this.CountData = await _addressRepository.GetCountByLocationWithReading(location);

            this.ClassActive = CurrentReading?.Count == CountData ? "active" : "disabled";

            this.GenerateButton = await Button(location);
            this.GenerateSelect = await WaterReadingSelect();
            this.Location = location;


            foreach (var item in prevReading.PreviousReading)
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
                await BillCalculations();
                await ReturnWaterBillingValues();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw;
            }


        }


        //FOR WATER BILLING BY RESIDENT
        public async Task UnpaidWaterBillingByResident(string resident_ID)
        {
            var model = await _waterBillRepository.UnpaidResidentWaterBilling("1");

            foreach (var item in model.WaterBillAddress)
            {
                string _dueDateLong = "";
                string _dueDateShort = "";
                string dueDateFromDay = "";
                string dueDateToDay = "";
                string dueDateMonthLong = "";
                string dueDateMonthShort = "";

                if (DateTime.TryParse(item.Due_Date_From, out DateTime dueFrom))
                {
                    dueDateFromDay = dueFrom.ToString("dd");
                }

                if (DateTime.TryParse(item.Due_Date_To, out DateTime dueTo))
                {
                    dueDateToDay = dueTo.ToString("dd");
                    dueDateMonthLong = dueTo.ToString("MMMM");
                    dueDateMonthShort = dueTo.ToString("MMM");
                }

                //DUE DATE
                _dueDateLong = $"{dueDateFromDay}-{dueDateMonthLong}-{dueDateToDay}";
                _dueDateShort = $"{dueDateFromDay}-{dueDateMonthShort}-{dueDateToDay}";

                //Computation
                //amount += Convert.ToDouble(UnpaidWaterBill.FirstOrDefault()?.Amount);

                this.DueDateMonthShort.Add(dueDateMonthShort);
                this.DueDateLong.Add(_dueDateLong);
                this.DueDateShort.Add(_dueDateShort);
                //this.UnpaidAmount = amount.ToString("F2");

                var monthsLong = UnpaidMonthsLong.Any(month => month == _dueDateLong);

                if (monthsLong)//if true
                {
                    continue;
                }
                else
                {
                    UnpaidMonthsLong.Add(_dueDateLong);
                }
            }

        }

        private async Task MapToModel(ModelBinding prevReading, ModelBinding currentReading)
        {
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
        }

        private async Task ParseWaterReadingDates()
        {
            this.WRCurrentFirstDate = CurrentReading?.Count < 1 ? string.Empty : ParseStartDate(CurrentReading?[0].Date);
            this.WRCurrentLastDate = CurrentReading?.Count < 1 ? string.Empty : ParseLastDate(CurrentReading?[GetLastIndex(CurrentReading)].Date);
            this.WRCurrentMonth = CurrentReading?.Count < 1 ? string.Empty : "-" + ParseMonth(CurrentReading?[0].Date) + "-";

            this.WRPrevFirstDate = PreviousReading?.Count < 1 ? string.Empty : ParseStartDate(PreviousReading?[0].Date);//PreviousReading?[0].Date.ToString() ?? string.Empty;
            this.WRPrevLastDate = PreviousReading?.Count < 1 ? string.Empty : ParseLastDate(PreviousReading?[GetLastIndex(PreviousReading)].Date);
            this.WRPrevMonth = PreviousReading?.Count < 1 ? string.Empty : "-" + ParseMonth(PreviousReading?[0].Date) + "-";
        }

        private async Task BillCalculations()
        {
            for (int i = 0; i < ResidentAddress.Count; i++)
            {
                var cubic = 0.0;
                var amount = 0.0;
                double previousConsumption = 0;
                double currentConsumption = 0;
                double previousWaterBillAmount = 0;
                double TotalAmount = 0.0;

                // Check ARREARS
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

        private async Task ReturnWaterBillingValues()
        {
            // Define default values for cases where data might be missing
            for (int i = 0; i < ResidentAddress.Count; i++)
            {
                var listHTML = new List<HTMLValueForWaterBilling>();
                string previousReading = "N/A";
                string currentReading = "N/A";
                string cubicMeter = "N/A";
                string dateTextList = "N/A";
                string billAmount = "N/A";
                string waterBillNumber = "N/A";
                string prevWaterBill = "";
                string prevWaterBillAmount = "N/A";
                string total = "N/A";
                string dueDate = "";
                string dateIssued = "";

                var _issuedDate = "";
                var dateIssueFrom = "";
                var dateIssueTo = "";
                var dateIssuedMonth = "";

                var _dueDate = "";
                var dueDateFromDay = "";
                var dueDateToDay = "";
                var dueDateMonth = "";

                //DUE DATE
                if (i < WaterBill?.Count)
                {
                    if (DateTime.TryParse(WaterBill[i].Due_Date_From, out DateTime dueFrom))
                    {
                        dueDateFromDay = dueFrom.ToString("dd");
                    }

                    if (DateTime.TryParse(WaterBill[i].Due_Date_To, out DateTime dueTo))
                    {
                        dueDateToDay = dueTo.ToString("dd");
                        dueDateMonth = dueTo.ToString("MMM");
                    }
                    //DUE DATE
                    _dueDate = $"{dueDateFromDay}-{dueDateMonth}-{dueDateToDay}";
                    this.DueDate.Add(_dueDate);
                }

                //DATE ISSUE
                if (i < WaterBill?.Count)
                {
                    if (DateTime.TryParse(WaterBill[i].Date_Issue_From, out DateTime issuedFrom))
                    {
                        dateIssueFrom = issuedFrom.ToString("dd");
                    }

                    if (DateTime.TryParse(WaterBill[i].Date_Issue_To, out DateTime issuedTo))
                    {
                        dateIssueTo = issuedTo.ToString("dd");
                        dateIssuedMonth = issuedTo.ToString("MMM");
                    }
                    //DUE DATE
                    _issuedDate = $"{dateIssueFrom}-{dateIssuedMonth}-{dateIssueTo}";
                    this.DateIssued.Add(_issuedDate);
                }

                if (i < DueDate.Count)
                {
                    dueDate = DueDate[i] ?? "";
                }

                if (i < DateIssued.Count)
                {
                    dateIssued = DateIssued[i] ?? "";
                }

                if (i < Total.Count)
                {
                    total = Total[i].ToString("F2");
                }

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

                // Fetch the WBDateTextList if available
                if (i < WBDateTextList.Count)
                {
                    dateTextList = WBDateTextList[i] ?? "N/A";
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

                if (i < PreviousBillAmount.Count)
                {
                    prevWaterBillAmount = PreviousBillAmount[i].ToString("F2");
                }
                if (i < UnpaidWaterBill.Count)
                {
                    prevWaterBill = UnpaidWaterBill[i]?.PreviousWaterBill ?? "N/A";
                }

                WaterBillingValues.Add(
                    new HTMLValueForWaterBilling
                    {
                        PreviousReading = previousReading,
                        CurrentReading = currentReading,
                        CubicMeter = cubicMeter,
                        DateTextList = dateTextList,
                        BillAmount = billAmount,
                        WaterBillNumber = waterBillNumber,
                        PrevWaterBill = prevWaterBill,
                        PrevWaterBillAmount = prevWaterBillAmount,
                        Total = total,
                        DueDate = dueDate,
                        DateIssued = dateIssued
                    });

            }
        }

        private async Task<HtmlString> Button(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                return new HtmlString("");
            }

            string button = "<button id=\"btnGenerate\" class=\"mt-3 mb-3 p-2 btn btn-primary " + ClassActive + " \"> Generate Bill </button>";

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
            for (int i = ReadingStartDateRange.Count - 1; i >= 0; i--)
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

                var selected = i == ReadingStartDateRange.Count - 1 ? "selected" : "";
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

        public async Task<DataTable> ReportWaterBilling(List<ReportWaterBilling> reportWaterBilling)
        {
            // Adjust your implementation here to loop through the list of reportWaterBilling items
            // and fetch the corresponding data.

            var dt = new DataTable();

            if (reportWaterBilling.Count < 1 || reportWaterBilling == null)
            {
                return dt;
            }
            try
            {
                // Setup DataTable columns
                dt.Columns.Add("WaterBill_ID");
                dt.Columns.Add("WaterBill_No");
                dt.Columns.Add("Block");
                dt.Columns.Add("Lot");
                dt.Columns.Add("Name");
                dt.Columns.Add("Previous");
                dt.Columns.Add("Current");
                dt.Columns.Add("DateBillMonth");
                dt.Columns.Add("Consumption");
                dt.Columns.Add("Rate");
                dt.Columns.Add("Date_Issue");
                dt.Columns.Add("Due_Date");
                dt.Columns.Add("Previous_WaterBill");
                dt.Columns.Add("Amount_Due_Now");
                dt.Columns.Add("Amount_Due_Previous");
                dt.Columns.Add("Total");

                // Loop through each reportWaterBilling object and process data
                foreach (var report in reportWaterBilling)
                {
                    var prevReading = await _waterBillRepository.GetPreviousReadingReport(report);
                    var currentReading = await _waterBillRepository.GetCurrentReadingReport(report);
                    this.UnpaidWaterBill = await _waterBillRepository.GetUnpaidWaterBill(currentReading.CurrentReading);

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

                    await BillCalculations();
                    await ReturnWaterBillingValues();


                    // Sample row creation (replace with your actual data processing logic)
                    DataRow row = dt.NewRow();
                    row["WaterBill_ID"] = currentReading.WBilling?.FirstOrDefault()?.WaterBill_ID ?? "";
                    row["WaterBill_No"] = report.WaterBill_Number;
                    row["Block"] = prevReading.ResidentAddress?.FirstOrDefault()?.Block ?? "";
                    row["Lot"] = prevReading.ResidentAddress?.FirstOrDefault()?.Lot ?? "";
                    row["Name"] = prevReading.ResidentAddress?.FirstOrDefault()?.Name ?? "";
                    row["Previous"] = WaterBillingValues?.FirstOrDefault()?.PreviousReading ?? "";
                    row["Current"] = WaterBillingValues?.FirstOrDefault()?.CurrentReading ?? "";
                    row["DateBillMonth"] = report.DateBillMonth ?? "";
                    row["Consumption"] = WaterBillingValues?.FirstOrDefault()?.CubicMeter;
                    row["Rate"] = this.WaterRate; // Replace with actual data
                    row["Date_Issue"] = WaterBillingValues?.FirstOrDefault()?.DateIssued;
                    row["Due_Date"] = WaterBillingValues?.FirstOrDefault()?.DueDate ?? ""; // DUE DATE
                    row["Previous_WaterBill"] = WaterBillingValues?.FirstOrDefault()?.PrevWaterBill ?? "N/A"; // Replace with actual data
                    row["Amount_Due_Now"] = WaterBillingValues?.FirstOrDefault()?.BillAmount; // Replace with actual data
                    row["Amount_Due_Previous"] = WaterBillingValues?.FirstOrDefault()?.PrevWaterBillAmount ?? ""; // Replace with actual data
                    row["Total"] = WaterBillingValues?.FirstOrDefault()?.Total; // Replace with actual data

                    dt.Rows.Add(row);
                }

                return dt;
            }
            catch (Exception ex)
            {
                // Handle exception (logging, rethrow, etc.)
                throw new Exception($"Error generating report data: {ex.Message}");
            }
        }

        public async Task Trash(List<ReportWaterBilling> reportWaterBilling)
        {
            foreach (var report in reportWaterBilling)
            {
                var prevReading = await _waterBillRepository.GetPreviousReadingReport(report);
                var currentReading = await _waterBillRepository.GetCurrentReadingReport(report);
                this.UnpaidWaterBill = await _waterBillRepository.GetUnpaidWaterBill(prevReading.PreviousReading);

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

                ReturnWaterBillingValues();

                var cubic = 0.0;
                var amount = 0.0;
                double previousConsumption = 0;
                double currentConsumption = 0;
                double previousWaterBillAmount = 0;
                double TotalAmount = 0.0;
                var date = "";
                var dueDateFrom = currentReading.WBilling.FirstOrDefault()?.Due_Date_From;
                var dueDateTo = currentReading.WBilling.FirstOrDefault()?.Due_Date_To;
                var dueDateFromDay = "";
                var dueDateToDay = "";
                var dueDateMonth = "";
                var DueDate = "";

                if (DateTime.TryParse(dueDateFrom, out DateTime dueFrom))
                {
                    dueDateFromDay = dueFrom.ToString("dd");
                }

                if (DateTime.TryParse(dueDateTo, out DateTime dueTo))
                {
                    dueDateToDay = dueTo.ToString("dd");
                    dueDateMonth = dueTo.ToString("MMM");
                }

                if (DateTime.TryParse(prevReading.PreviousReading.FirstOrDefault()?.Date, out DateTime result))
                {
                    date = result.ToString("MMM yyyy");
                }

                // Check if the index is within range for PreviousReading
                if (!double.TryParse(UnpaidWaterBill.FirstOrDefault()?.PreviousWaterBillAmount, out previousWaterBillAmount))
                {
                    previousWaterBillAmount = 0; // Default value if parsing fails
                }

                // Check if the index is within range for PreviousReading
                if (!double.TryParse(prevReading.PreviousReading?.FirstOrDefault()?.Consumption, out previousConsumption))
                {

                    previousConsumption = 0; // Default value if parsing fails
                }

                // Check if the index is within range for CurrentReading
                if (!double.TryParse(currentReading.CurrentReading?.FirstOrDefault()?.Consumption, out currentConsumption))
                {
                    currentConsumption = 0; // Default value if parsing fails
                }

                // Calculate cubic difference
                cubic = (currentConsumption - previousConsumption) < 1 ? 0 : currentConsumption - previousConsumption;

                // Calculate bill amount
                amount = cubic * WaterRate;

                //Calculate Total Amount
                TotalAmount = amount + previousWaterBillAmount;

                //DUE DATE
                DueDate = $"{dueDateFromDay}-{dueDateMonth}-{dueDateToDay}";


                // Sample row creation (replace with your actual data processing logic)
                //DataRow row = dt.NewRow();
                //row["WaterBill_ID"] = currentReading.WBilling?.FirstOrDefault()?.WaterBill_ID ?? ""; // Replace with actual data
                //row["WaterBill_No"] = report.WaterBill_Number;
                //row["Block"] = prevReading.ResidentAddress?.FirstOrDefault()?.Block ?? ""; // Replace with actual data
                //row["Lot"] = prevReading.ResidentAddress?.FirstOrDefault()?.Lot ?? ""; // Replace with actual data
                //row["Name"] = prevReading.ResidentAddress?.FirstOrDefault()?.Name ?? ""; // Replace with actual data
                //row["Previous"] = prevReading.PreviousReading?.FirstOrDefault()?.Consumption ?? "";
                //row["Current"] = currentReading.CurrentReading?.FirstOrDefault()?.Consumption ?? "";
                //row["DateBillMonth"] = date ?? ""; // Replace with actual data
                //row["Consumption"] = cubic; // Replace with actual data
                //row["Rate"] = this.WaterRate; // Replace with actual data
                //row["Date_Issue"] = "Sample Date Issue"; // Replace with actual data
                //row["Due_Date"] = DueDate ?? ""; // DUE DATE
                //row["Previous_WaterBill"] = UnpaidWaterBill?.FirstOrDefault()?.PreviousWaterBill ?? "N/A"; // Replace with actual data
                //row["Amount_Due_Now"] = amount.ToString("F2"); // Replace with actual data
                //row["Amount_Due_Previous"] = previousWaterBillAmount.ToString("F2") ?? "0.00"; // Replace with actual data
                //row["Total"] = TotalAmount; // Replace with actual data

                //dt.Rows.Add(row);
            }
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