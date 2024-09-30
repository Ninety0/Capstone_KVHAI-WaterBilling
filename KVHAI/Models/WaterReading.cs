namespace KVHAI.Models
{
    public class WaterReading
    {
        public string Reading_ID { get; set; } = string.Empty;
        public string Emp_ID { get; set; } = string.Empty;
        public string Address_ID { get; set; } = string.Empty;
        public string Consumption { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;

        public string CubicConsumption { get; set; } = string.Empty;
        public string DateMonth { get; set; } = string.Empty;
        public List<string> DateYear { get; set; } = new List<string>();
        public string WaterBill_Number { get; set; } = string.Empty;
        public string Resident_ID { get; set; } = string.Empty;

    }
}
