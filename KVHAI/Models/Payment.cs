namespace KVHAI.Models
{
    public class Payment
    {
        public int Payment_ID { get; set; }
        public int Emp_ID { get; set; }
        public int Address_ID { get; set; }
        public int Resident_ID { get; set; }
        public decimal Bill { get; set; }
        public decimal Paid_Amount { get; set; }
        public decimal Remaining_Balance { get; set; }
        public string? Payment_Method { get; set; }
        public string? Payment_Status { get; set; }
        public string? Payment_Date { get; set; }
        public string? Paid_By { get; set; }

        public string Block { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public Boolean Is_Owner { get; set; }

    }
}
