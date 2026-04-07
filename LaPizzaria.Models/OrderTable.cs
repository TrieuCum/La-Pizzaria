namespace LaPizzaria.Models
{
	public class OrderTable
	{
		public int OrderId { get; set; }
		public Order? Order { get; set; }

		public int TableId { get; set; }
		public Table? Table { get; set; }
	}
}


