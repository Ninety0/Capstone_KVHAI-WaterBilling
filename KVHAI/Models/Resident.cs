namespace KVHAI.Models
{
    public class Resident
    {
        public string Res_ID { get; set; } = string.Empty;

        public string Lname { get; set; } = string.Empty;

        public string Fname { get; set; } = string.Empty;

        public string Mname { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Occupancy { get; set; } = string.Empty;

        public string Verification_Token { get; set; } = string.Empty;

        public DateTime? Verified_At { get; set; }

        public string Password_Reset_Token { get; set; } = string.Empty;

        public DateTime? Reset_Token_Expire { get; set; }

        public string Date_Residency { get; set; } = string.Empty;

        public string Activated { get; set; } = string.Empty;

        public string Block { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;




    }
}
