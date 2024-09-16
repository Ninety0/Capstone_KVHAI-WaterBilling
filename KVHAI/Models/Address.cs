namespace KVHAI.Models
{
    public class Address
    {
        public int Address_ID { get; set; }
        public int Resident_ID { get; set; }
        public string Block { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public int Street_ID { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Remove_Request_Token { get; set; } = string.Empty;
        public string Remove_Token_Date { get; set; } = string.Empty;

        public string Resident_Name { get; set; } = string.Empty;
        public string Street_Name { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

    }
}
