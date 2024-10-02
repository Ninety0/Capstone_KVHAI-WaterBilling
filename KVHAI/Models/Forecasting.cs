namespace KVHAI.Models
{
    public class Forecasting
    {
        public int Forecast_ID { get; set; }
        public List<double?> Moving_Average { get; set; }
        public List<double> Actual_Data { get; set; }
        public List<string?> Insights { get; set; }
        public List<double?> Percent_Change { get; set; }
    }
}
