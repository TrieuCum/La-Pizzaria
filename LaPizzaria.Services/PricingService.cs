using System;
using LaPizzaria.Models;

namespace LaPizzaria.Services
{
    public class PricingService : IPricingService
    {
        public decimal AdjustUnitPrice(Product product, decimal basePrice, DateTime at)
        {
            // Example: 10% off during happy hour (14-16 UTC)
            if (at.Hour >= 14 && at.Hour < 16)
            {
                return Math.Round(basePrice * 0.9m, 2);
            }
            return basePrice;
        }
    }
}


