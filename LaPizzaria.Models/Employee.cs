using System.ComponentModel.DataAnnotations;

namespace LaPizzaria.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        public decimal? Salary { get; set; }

        public DateTime HireDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Đang làm";

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
