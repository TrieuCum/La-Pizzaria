using System;
using System.Collections.Generic;

namespace LaPizzaria.Models
{
	public class Table
	{
		public int Id { get; set; }
		public string Code { get; set; } = string.Empty; // e.g., T01
		public int Capacity { get; set; }
		public bool IsActive { get; set; } = true;
		public bool IsOccupied { get; set; } = false;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

		// Navigation
		public ICollection<OrderTable> OrderTables { get; set; } = new List<OrderTable>();
	}
}


