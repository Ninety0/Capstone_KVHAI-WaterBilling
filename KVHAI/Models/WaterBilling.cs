namespace KVHAI.Models
{
    public class WaterBilling
    {
        public int WaterBill_ID { get; set; }
        public int Address_ID { get; set; }
        public string Amount { get; set; } = string.Empty;
        public string Date_Issue_From { get; set; } = string.Empty;
        public string Date_Issue_To { get; set; } = string.Empty;
        public string Due_Date_From { get; set; } = string.Empty;
        public string Due_Date_To { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string WaterBill_No { get; set; } = string.Empty;


    }
}
