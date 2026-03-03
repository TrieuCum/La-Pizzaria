using System.Collections.Generic;
using System.Linq;
using LaPizzaria.Models;

namespace LaPizzaria.ViewModels
{
    public class TableIndexViewModel
    {
        public IEnumerable<Table> Tables { get; set; } = new List<Table>();
        public IEnumerable<Order> OpenOrders { get; set; } = new List<Order>();
        public Dictionary<int, string> TableAttachInfo { get; set; } = new Dictionary<int, string>();

        public int TotalTables => Tables.Count();
        public int OccupiedTables => Tables.Count(t => t.IsOccupied);
        public int OccupancyRate => TotalTables > 0 ? (OccupiedTables * 100 / TotalTables) : 0;
        public int FreeSeats => Tables.Where(t => !t.IsOccupied).Sum(t => t.Capacity);
    }
}
