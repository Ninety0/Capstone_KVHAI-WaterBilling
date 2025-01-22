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
        public List<Notification>? NotificationStaff { get; set; }
        public int CountNotificationResident { get; set; }
        public int CountData { get; set; }
        public List<WaterBillWithAddress>? UnpaidResidentWaterBilling { get; set; }
        public List<WaterBillWithAddress>? PaidResidentWaterBilling { get; set; }

        public Pagination<Employee>? EmployeePagination { get; set; }
        public Pagination<Announcement>? AnnouncementPagination { get; set; }
        public Pagination<Streets>? StreetPagination { get; set; }
        public Pagination<AddressWithResident>? ResidentPagination { get; set; }
        public Pagination<Payment>? PaymentPagination { get; set; }
        public Pagination<WaterBilling>? WaterBillingPagination { get; set; }

        public List<WaterReading>? PreviousReading { get; set; }
        public List<WaterReading>? CurrentReading { get; set; }
        public List<ResidentAddress>? ResidentAddress { get; set; }
        public List<Payment>? PaymentList { get; set; }
        public List<WaterBilling>? WBilling { get; set; }

        public List<WaterBilling> Bill { get; set; }
        public string Date { get; set; } = string.Empty;

        public List<ReportWaterBilling>? Items { get; set; } = new List<ReportWaterBilling>();
        public List<Payment>? ItemPayment { get; set; } = new List<Payment>();
        public Payment Payment { get; set; }
        public string FileType { get; set; } = string.Empty;

        public List<Streets>? ListStreet { get; set; }
        public List<Address>? ListAddress { get; set; }

        public List<WaterBillWithAddress>? WaterBillAddress { get; set; }

        public List<WaterReading>? AllWaterConsumptionByResident { get; set; }
        public List<ResidentAddress> RequestAddressList { get; set; }
        public List<Renter> RenterList { get; set; }
        public List<RequestDetails> RequestDetailList { get; set; }

        public List<int> YearList = new List<int>();

        public List<Resident> ResidentList { get; set; }
        public List<WaterReading> WaterReadingList { get; set; }
        public List<WaterBilling> WaterBillingList { get; set; }
        public List<Employee> EmployeeList { get; set; }
        public List<Address> AddressList { get; set; }
        public string AddressCountByLocation { get; set; } = string.Empty;
        public string ReadingCountByLocation { get; set; } = string.Empty;


    }
}
