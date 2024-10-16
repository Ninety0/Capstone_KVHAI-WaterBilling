namespace KVHAI.Models
{
    public class Forecasting
    {
        public Dictionary<int, YearData> YearlyData { get; set; } = new Dictionary<int, YearData>();

        //public int Forecast_ID { get; set; }
        //public List<double?> Moving_Average { get; set; }
        //public List<double> Actual_Data { get; set; }
        //public List<string?> Insights { get; set; }
        //public List<double?> Percent_Change { get; set; }
    }

    public class YearData
    {
        public List<double?> ActualData { get; set; }
        public List<double?> MovingAverage { get; set; }
        public List<double?> PercentChange { get; set; }
        public List<string> Insights { get; set; }
        public List<int> ConsumptionMonth { get; set; }

    }

    public class ConsumptionDataSet
    {
        public List<int> ConsumptionData { get; set; }
    }
}
