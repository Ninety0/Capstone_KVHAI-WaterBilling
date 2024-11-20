namespace KVHAI.Models
{
    public class HubConnect
    {
        public int Id { get; set; }
        public string Resident_ID { get; set; } = null!;
        public string Employee_ID { get; set; }
        public string Connection_ID { get; set; } = null!;
        public string Username { get; set; } = null!;
    }
}
