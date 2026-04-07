using System;
using System.Collections.Generic;
using System.Linq;
using LaPizzaria.Models;

namespace LaPizzaria.Services
{
	public static class ProductPricingCalculator
	{
		private const decimal SizeSmallFactor = 0.8m;
		private const decimal SizeMediumFactor = 1m;
		private const decimal SizeLargeFactor = 1.2m;

		public static decimal ComputeCost(IEnumerable<(decimal QuantityPerUnit, decimal UnitPrice)> lines)
		{
			return lines.Sum(x => Math.Round(x.QuantityPerUnit * x.UnitPrice, 4, MidpointRounding.AwayFromZero));
		}

		public static decimal RoundSalePriceVnd(decimal amount)
		{
			if (amount <= 0) return 0m;
			return Math.Round(amount / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
		}

		public static decimal SalePriceFromCost(decimal ingredientCost, decimal profitMarginPercent)
		{
			if (ingredientCost <= 0) return 0m;
			var margin = profitMarginPercent;
			if (margin < 30m) margin = 30m;
			if (margin > 50m) margin = 50m;
			return RoundSalePriceVnd(ingredientCost * (1m + margin / 100m));
		}

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
}
