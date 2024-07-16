using KVHAI.CustomClass;

namespace KVHAI.Models
{
    public class ModelBinding
    {
        public IEnumerable<Resident>? Residents { get; set; }
        public IEnumerable<Employee>? Employees { get; set; }

        public Pagination<Employee>? EmployeePagination { get; set; }
        public Pagination<Resident>? ResidentPagination { get; set; }
    }
}
