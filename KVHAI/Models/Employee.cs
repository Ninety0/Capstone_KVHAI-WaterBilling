using System.ComponentModel.DataAnnotations;

namespace KVHAI.Models
{
    public class Employee
    {
        public string Emp_ID   { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Lname    { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string Fname    { get; set; } = string.Empty;

        [Required] 
        [StringLength(50)] 
        public string Mname    { get; set; } = string.Empty;
        
        [Required] [StringLength(50)] 
        public string Phone    { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email    { get; set; } = string.Empty;
        
        [Required] 
        [StringLength(50)] 
        public string Username { get; set; } = string.Empty;
        
        [Required] 
        [StringLength(50)] 
        public string Password { get; set; } = string.Empty;
        
        [Required] 
        [StringLength(50)] 
        public string Role { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Created_At { get; set; } = string.Empty;
    }
}
