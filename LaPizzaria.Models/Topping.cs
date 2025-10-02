using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaPizzaria.Models
{
    public class Topping
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? ImageUrl { get; set; }

        // Navigation properties
        public ICollection<ProductTopping> ProductToppings { get; set; } = new List<ProductTopping>();
        public ICollection<OrderDetailTopping> OrderDetailToppings { get; set; } = new List<OrderDetailTopping>();
    }
}
