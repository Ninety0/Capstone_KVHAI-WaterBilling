namespace KVHAI.Models
{
    public class ResidentAddress
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Block { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Register_At { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public int Address_ID { get; set; }
        public int Resident_ID { get; set; }
        public int Street_ID { get; set; }
        public List<string>? ListOfBlock { get; set; }
        public List<string>? ListOfLot { get; set; }
        public List<string>? ListOfStreet { get; set; }

        public int Resident_Address_ID { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Payment { get; set; }
        public int Is_Owner { get; set; }
        public string? Status { get; set; }
        public string? Request_Date { get; set; }
        public string? Decision_Date { get; set; }
    }
}
