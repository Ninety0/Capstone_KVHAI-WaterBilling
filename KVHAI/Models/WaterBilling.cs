namespace KVHAI.Models
{
    public class WaterBilling
    {
        public int WaterBill_ID { get; set; }
        public int Reading_ID { get; set; }
        public string Amount { get; set; } = string.Empty;
        public string Date_Billing { get; set; } = string.Empty;
        public string Due_Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
