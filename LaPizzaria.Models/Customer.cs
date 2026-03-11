using System.ComponentModel.DataAnnotations;

namespace LaPizzaria.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? FullName { get; set; }

        [Required]
        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public int Points { get; set; } = 0;

        // Nếu sau này bạn muốn biết khách hàng này đã đặt những đơn nào
        public virtual ICollection<Order>? Orders { get; set; }
    }
}