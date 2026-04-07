using System;
using System.Collections.Generic;
using System.Linq;

namespace LaPizzaria.Services
{
	/// <summary>
	/// Giá vốn 1 phần món = Σ (số lượng nguyên liệu × đơn giá đơn vị). Giá bán = vốn × (1 + % lời/100).
	/// </summary>
	public static class ProductPricingCalculator
	{
		public static decimal ComputeCost(IEnumerable<(decimal QuantityPerUnit, decimal UnitPrice)> lines)
		{
			return lines.Sum(x => Math.Round(x.QuantityPerUnit * x.UnitPrice, 4, MidpointRounding.AwayFromZero));
		}

		/// <summary>Làm tròn giá bán theo hàng nghìn đồng (VD: 127.340 ₫ → 127.000 ₫).</summary>
		public static decimal RoundSalePriceVnd(decimal amount)
		{
			if (amount <= 0) return 0m;
			return Math.Round(amount / 1000m, 0, MidpointRounding.AwayFromZero) * 1000m;
		}

		public static decimal SalePriceFromCost(decimal ingredientCost, decimal profitMarginPercent)
		{
			if (ingredientCost <= 0) return 0m;
			// % lời nghiệp vụ: 30–50 (khớp form sản phẩm)
			var m = profitMarginPercent;
			if (m < 30m) m = 30m;
			else if (m > 50m) m = 50m;
			var raw = ingredientCost * (1m + m / 100m);
			return RoundSalePriceVnd(raw);
		}
	}
}
