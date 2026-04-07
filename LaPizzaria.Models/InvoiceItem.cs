namespace LaPizzaria.Models
{
	public class InvoiceItem
	{
		public int Id { get; set; }
		public int InvoiceId { get; set; }
		public Invoice? Invoice { get; set; }

		public int OrderDetailId { get; set; }
		public OrderDetail? OrderDetail { get; set; }

		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal Discount { get; set; }
		public decimal Total { get; set; }
	}
}


