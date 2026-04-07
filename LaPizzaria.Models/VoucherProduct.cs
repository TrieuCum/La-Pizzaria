namespace LaPizzaria.Models
{
    public class VoucherProduct
    {
        public int VoucherId { get; set; }
        public int ProductId { get; set; }

        public Voucher? Voucher { get; set; }
        public Product? Product { get; set; }
    }
}
