using System;
using System.Collections.Generic;

namespace LaPizzaria.Models
{
	public class Invoice
	{
		public int Id { get; set; }
		public int OrderId { get; set; }
		public Order? Order { get; set; }
		public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
		public decimal Subtotal { get; set; }
		public decimal DiscountTotal { get; set; }
		public decimal TaxTotal { get; set; }
		public decimal GrandTotal { get; set; }

		public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
	}
}


