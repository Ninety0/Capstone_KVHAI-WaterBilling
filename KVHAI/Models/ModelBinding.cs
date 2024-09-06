using KVHAI.CustomClass;

namespace KVHAI.Models
{
    public class ModelBinding
    {
        public IEnumerable<Resident>? Residents { get; set; }
        public IEnumerable<Employee>? Employees { get; set; }
        public List<Resident>? ResidentsAddress { get; set; }

        public Pagination<Employee>? EmployeePagination { get; set; }
        public Pagination<Resident>? ResidentPagination { get; set; }

        public List<WaterReading>? PreviousReading { get; set; }
        public List<WaterReading>? CurrentReading { get; set; }
        public List<ResidentAddress>? ResidentAddress { get; set; }
        public List<WaterBilling>? WBilling { get; set; }

        public List<WaterBilling> Bill { get; set; }
        public string Date { get; set; } = string.Empty;

        public List<ReportWaterBilling>? Items { get; set; } = new List<ReportWaterBilling>();
        public string FileType { get; set; } = string.Empty;
    }
}
