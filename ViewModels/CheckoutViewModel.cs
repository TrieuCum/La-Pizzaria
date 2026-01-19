using System.ComponentModel.DataAnnotations;

namespace LaPizzaria.ViewModels
{
    public class CheckoutViewModel
    {
        [Required]
        public string DeliveryType { get; set; } = "DineIn";

        public string? DeliveryAddress { get; set; }

        public string? Notes { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "Cash";

        public string? VoucherCode { get; set; }
    }
}
