using KVHAI.CustomClass;

namespace KVHAI.Models
{
    public class ModelBinding
    {
        public string Resident_ID { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public IEnumerable<Resident>? Residents { get; set; }
        public IEnumerable<Employee>? Employees { get; set; }
        public List<Resident>? ResidentsAddress { get; set; }
        public List<Announcement>? AnnouncementList { get; set; }
        public List<Notification>? NotificationResident { get; set; }
        public int CountNotificationResident { get; set; }
        public List<WaterBillWithAddress>? UnpaidResidentWaterBilling { get; set; }

        public Pagination<Employee>? EmployeePagination { get; set; }
        public Pagination<AddressWithResident>? ResidentPagination { get; set; }

        public List<WaterReading>? PreviousReading { get; set; }
        public List<WaterReading>? CurrentReading { get; set; }
        public List<ResidentAddress>? ResidentAddress { get; set; }
        public List<Payment>? PaymentList { get; set; }
        public List<WaterBilling>? WBilling { get; set; }

        public List<WaterBilling> Bill { get; set; }
        public string Date { get; set; } = string.Empty;

        public List<ReportWaterBilling>? Items { get; set; } = new List<ReportWaterBilling>();
        public string FileType { get; set; } = string.Empty;

        public List<Streets>? ListStreet { get; set; }
        public List<Address>? ListAddress { get; set; }

        public List<WaterBillWithAddress>? WaterBillAddress { get; set; }

        public List<WaterReading>? AllWaterConsumptionByResident { get; set; }
        public List<ResidentAddress> RequestAddressList { get; set; }


    }
}
