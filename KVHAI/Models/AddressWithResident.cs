namespace KVHAI.Models
{
    public class AddressWithResident
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

        public string Is_Activated { get; set; } = string.Empty;

        public string Account_Number { get; set; } = string.Empty;
        public string Date_Residency { get; set; } = string.Empty;
        public string Verified_At { get; set; } = string.Empty;

        public string _Block { get; set; } = string.Empty;
        public string _Lot { get; set; } = string.Empty;
        public string _NameStreet { get; set; } = string.Empty;


        public List<string>? AddressID { get; set; } = new List<string>();
        public List<string>? Block { get; set; } = new List<string>();
        public List<string>? Lot { get; set; } = new List<string>();
        public List<string>? Street_Name { get; set; } = new List<string>();
        public List<string>? Is_Verified { get; set; } = new List<string>();

    }
}
