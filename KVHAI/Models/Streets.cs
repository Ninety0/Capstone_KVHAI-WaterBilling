using System.ComponentModel.DataAnnotations.Schema;

namespace KVHAI.Models
{
    [Table("street_tb")]
    public class Streets
    {
        [Column("st_id")]
        public string Street_ID { get; set; } = string.Empty;

        [Column("st_name")]
        public string Street_Name { get; set; } = string.Empty;
    }
}
