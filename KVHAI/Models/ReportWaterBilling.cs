namespace KVHAI.Models
{
    public class ReportWaterBilling
    {
        public int WaterBill_ID { get; set; }
        public int CurrentReading_ID { get; set; }
        public int WaterBill_Number { get; set; }
        public string Block { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Previous { get; set; } = string.Empty;
        public string DateBillMonth { get; set; } = string.Empty;
        public string Current { get; set; } = string.Empty;
        public string Consumption { get; set; } = string.Empty;
        public string Rate { get; set; } = string.Empty;
        public string Date_Issue { get; set; } = string.Empty;
        public string Due_Date { get; set; } = string.Empty;
        public string Previous_WaterBill { get; set; } = string.Empty;
        public string Amount_Due_Now { get; set; } = string.Empty;
        public string Amount_Due_Previous { get; set; } = string.Empty;
        public string Total { get; set; } = string.Empty;


    }
}
