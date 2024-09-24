namespace KVHAI.Models
{
    public class WaterBillWithAddress
    {
        public string WaterBill_ID { get; set; } = string.Empty;
        public string Address_ID { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Date_Issue_From { get; set; } = string.Empty;
        public string Date_Issue_To { get; set; } = string.Empty;
        public string Due_Date_From { get; set; } = string.Empty;
        public string Due_Date_To { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string WaterBill_No { get; set; } = string.Empty;
        public int Resident_ID { get; set; }
        public string Block { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public int Street_ID { get; set; }
        public string Resident_Name { get; set; } = string.Empty;
        public string Street_Name { get; set; } = string.Empty;

        public string TotalAmount { get; set; } = string.Empty;
    }
}
