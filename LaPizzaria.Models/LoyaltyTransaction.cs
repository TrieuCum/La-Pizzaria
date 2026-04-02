using System;
using System.ComponentModel.DataAnnotations;

namespace LaPizzaria.Models
{
    public class LoyaltyTransaction
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        // + points for earn, - points for redeem
        public int PointsChange { get; set; }
        public int BalanceAfter { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } = "Earn"; // Earn | Spend | Adjust

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        // Used to make earning from order idempotent (ex: ORDER:123)
        [StringLength(100)]
        public string? ReferenceCode { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
    }
}
