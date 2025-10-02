using System;
using LaPizzaria.Models;

namespace LaPizzaria.Services
{
    public interface IPricingService
    {
        decimal AdjustUnitPrice(Product product, decimal basePrice, DateTime at);
    }
}


