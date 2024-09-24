namespace KVHAI.Models
{
    public class Announcement
    {
        public int Announcement_ID { get; set; }
        public int Employee_ID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Contents { get; set; } = string.Empty;
        public string Date_Created { get; set; } = string.Empty;
        public string Date_Expire { get; set; } = string.Empty;
    }
}
