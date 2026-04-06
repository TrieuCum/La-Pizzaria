using LaPizzaria.Models;

namespace LaPizzaria.Services;

public static class ProductPricingCalculator
{
    private const decimal SizeSmallFactor = 0.8m;
    private const decimal SizeMediumFactor = 1.0m;
    private const decimal SizeLargeFactor = 1.2m;

    public static decimal GetSizeFactor(string? size)
    {
        if (string.Equals(size, "S", StringComparison.OrdinalIgnoreCase)) return SizeSmallFactor;
        if (string.Equals(size, "L", StringComparison.OrdinalIgnoreCase)) return SizeLargeFactor;
        return SizeMediumFactor;
    }

    public static decimal GetScaledQuantity(ProductIngredient mapping, Ingredient ingredient, string? size)
    {
        if (ingredient.IsDoughBase)
        {
            return 1m;
        }

        return mapping.QuantityPerUnit * GetSizeFactor(size);
    }
}
