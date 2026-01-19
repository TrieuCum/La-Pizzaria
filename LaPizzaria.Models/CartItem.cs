using System;

namespace LaPizzaria.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? SessionId { get; set; } // For anonymous users
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Product Product { get; set; } = null!;

        // Computed property for UnitPrice (get from Product)
        public decimal UnitPrice => Product?.Price ?? 0;
    }
}
