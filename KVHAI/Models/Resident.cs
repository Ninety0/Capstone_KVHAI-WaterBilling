using System.ComponentModel.DataAnnotations;

namespace KVHAI.Models
{
    public class Resident
    {
        public string Res_ID { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Lname { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Fname { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Mname { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string Block { get; set; } = string.Empty;

        [Required]
        [StringLength(5)]
        public string Lot { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Password { get; set; } = string.Empty;


        [Required]
        [StringLength(50)]
        public string Date_Residency { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Occupancy { get; set; } = string.Empty;

        [StringLength(50)]
        public string Created_At { get; set; } = string.Empty;


        public string Activated { get; set; } = string.Empty;


    }
}
