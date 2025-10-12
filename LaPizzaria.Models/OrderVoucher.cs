namespace LaPizzaria.Models
{
	public class OrderVoucher
	{
		public int OrderId { get; set; }
		public int VoucherId { get; set; }
		public Order? Order { get; set; }
		public Voucher? Voucher { get; set; }
	}
}


