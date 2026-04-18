using System.Collections.Generic;

namespace LaPizzaria.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int? ProductId2 { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }

        // Navigation properties
        public Order? Order { get; set; }
        public Product? Product { get; set; }
        public Product? Product2 { get; set; }
        public string? Size { get; set; }
        /// <summary>Dòng sản phẩm miễn phí do đổi voucher tích điểm.</summary>
        public bool IsFreeByVoucher { get; set; }
        public ICollection<OrderDetailTopping> OrderDetailToppings { get; set; } = new List<OrderDetailTopping>();
    }
}
