namespace KVHAI.Models
{
    public class RequestDetails
    {
        public int Request_ID { get; set; }
        public int Resident_ID { get; set; }
        public int Address_ID { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
        public int Status { get; set; }
        public string Comments { get; set; } = string.Empty;
        public DateTime? StatusUpdated { get; set; }

        public string Resident_Name { get; set; } = string.Empty;
        public string Address_Name { get; set; } = string.Empty;

    }
}
