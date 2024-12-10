namespace KVHAI.Models
{
    public class WaterBilling
    {
        public string WaterBill_ID { get; set; } = string.Empty;
        public string Reference_No { get; set; } = string.Empty;
        public string Address_ID { get; set; } = string.Empty;
        public string Previous_Reading { get; set; } = string.Empty;
        public string Current_Reading { get; set; } = string.Empty;
        public string Cubic_Meter { get; set; } = string.Empty;
        public string Bill_For { get; set; } = string.Empty;
        public string Bill_Date_Created { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string Date_Issue_From { get; set; } = string.Empty;
        public string Date_Issue_To { get; set; } = string.Empty;
        public string Due_Date_From { get; set; } = string.Empty;
        public string Due_Date_To { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string WaterBill_No { get; set; } = string.Empty;
        public string Account_Number { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string DatePeriodCovered { get; set; } = string.Empty;
        public string PreviousWaterBill { get; set; } = string.Empty;
        public string PreviousWaterBillAmount { get; set; } = string.Empty;



    }
}
