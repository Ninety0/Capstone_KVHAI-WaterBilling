namespace KVHAI.Models
{
    public class LocationPercentage
    {
        public int ReadingCount { get; set; }
        public int TotalCount { get; set; }
        public string Location { get; set; } = string.Empty;
        public double Percentage { get; set; }
    }
}
