using System.ComponentModel.DataAnnotations.Schema;

namespace KVHAI.Models
{
    public class Notification
    {
        [Column("notif_id")]
        public int Notification_ID { get; set; }

        [Column("uid")]
        public string Resident_ID { get; set; } = string.Empty;

        public string Address_ID { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime Created_At { get; set; }
        public string Message_Type { get; set; } = string.Empty;
        public Boolean Is_Read { get; set; }

        public string Hours { get; set; }
        public List<int> ListResident_ID { get; set; }
        public List<string> ListEmployee_ID { get; set; }

    }
}
