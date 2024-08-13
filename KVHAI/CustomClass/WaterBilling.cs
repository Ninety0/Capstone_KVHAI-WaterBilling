using KVHAI.Models;
using KVHAI.Repository;

namespace KVHAI.CustomClass
{
    public class WaterBilling
    {
        private readonly WaterReadingRepository _waterReadingRepository;
        private readonly AddressRepository _addressRepository;

        private const double WaterRate = 18.0;

        public List<WaterReading>? PreviousReading { get; set; }
        public List<WaterReading>? CurrentReading { get; set; }
        public List<ResidentAddress>? ResidentAddress { get; set; }

        public List<Double> CubicMeter { get; set; }
        public List<Double> BillAmount { get; set; }

        public int CountData { get; set; }

        public string CurrentFirstDate = string.Empty;
        public string CurrentMonth = string.Empty;
        public string CurrentLastDate = string.Empty;

        public string PrevFirstDate = string.Empty;
        public string PrevMonth = string.Empty;
        public string PrevLastDate = string.Empty;

        public string ErrorMessage = string.Empty;

        int index = 1;
        public WaterBilling(WaterReadingRepository waterReadingRepository, AddressRepository addressRepository)
        {
            _waterReadingRepository = waterReadingRepository;
            _addressRepository = addressRepository;
            CubicMeter = new List<double>();
            BillAmount = new List<Double>();
        }

        public async Task UseWaterBilling(string location = "")
        {
            var prevReading = await _waterReadingRepository.GetPreviousReading(location);
            var currentReading = await _waterReadingRepository.GetCurrentReading(location);

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
            this.CurrentMonth = CurrentReading?.Count < 1 ? string.Empty : ParseMonth(CurrentReading?[0].Date);


            this.PrevFirstDate = PreviousReading?.Count < 1 ? string.Empty : ParseStartDate(PreviousReading?[0].Date);//PreviousReading?[0].Date.ToString() ?? string.Empty;
            this.PrevLastDate = PreviousReading?.Count < 1 ? string.Empty : ParseLastDate(PreviousReading?[GetLastIndex(PreviousReading)].Date);
            this.PrevMonth = PreviousReading?.Count < 1 ? string.Empty : ParseMonth(PreviousReading?[0].Date);

            this.CountData = await _addressRepository.GetCountByLocation(location);

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