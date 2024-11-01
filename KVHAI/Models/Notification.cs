using System.ComponentModel.DataAnnotations.Schema;

namespace KVHAI.Models
{
    public class Notification
    {
        [Column("notif_id")]
        public int Notification_ID { get; set; }

        [Column("res_id")]
        public string Resident_ID { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime Created_At { get; set; }
        public string Message_Type { get; set; } = string.Empty;
        public Boolean Is_Read { get; set; }

        public string Hours { get; set; }

    }
}
