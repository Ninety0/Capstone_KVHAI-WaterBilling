namespace KVHAI.Models
{
    public class ResidentAddress
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Block { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;

        public int Address_ID { get; set; }
        public List<string>? ListOfBlock { get; set; }
        public List<string>? ListOfLot { get; set; }
        public List<string>? ListOfStreet { get; set; }
    }
}
