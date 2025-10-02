namespace LaPizzaria.Models
{
    public class OrderDetailTopping
    {
        public int OrderDetailId { get; set; }
        public OrderDetail? OrderDetail { get; set; }

        public int ToppingId { get; set; }
        public Topping? Topping { get; set; }
        public int Quantity { get; set; } = 1; // Default quantity for a custom topping
    }
}
