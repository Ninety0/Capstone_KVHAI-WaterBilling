namespace KVHAI.Models
{
    public class AnnouncementImage
    {
        public int Image_ID { get; set; }
        public int Announcement_ID { get; set; }
        public string Image_Url { get; set; } = string.Empty;
    }
}
