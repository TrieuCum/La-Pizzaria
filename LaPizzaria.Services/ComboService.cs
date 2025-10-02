using System;
using System.Collections.Generic;
using System.Linq;
using LaPizzaria.Models;

namespace LaPizzaria.Services
{
    public class ComboService : IComboService
    {
        public ComboSuggestionResult SuggestBestCombos(IEnumerable<OrderDetail> orderDetails, IEnumerable<Combo> availableCombos)
        {
            var result = new ComboSuggestionResult();
            var detailsByProduct = orderDetails
                .GroupBy(d => d.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            foreach (var combo in availableCombos.Where(c => c.IsActive))
            {
                var maxApplicability = int.MaxValue;
                foreach (var item in combo.Items)
                {
                    detailsByProduct.TryGetValue(item.ProductId, out var qtyInOrder);
                    var canApplyForItem = item.MinQuantity == 0 ? qtyInOrder : qtyInOrder / Math.Max(1, item.MinQuantity);
                    maxApplicability = Math.Min(maxApplicability, canApplyForItem);
                }

                if (maxApplicability <= 0 || maxApplicability == int.MaxValue)
                {
                    continue;
                }

                var comboDiscount = 0m;
                if (combo.DiscountAmount > 0)
                {
                    comboDiscount += combo.DiscountAmount * maxApplicability;
                }
                if (combo.DiscountPercent.HasValue && combo.DiscountPercent.Value > 0)
                {
                    // approximate percent-based discount based on items price in order details
                    var affectedDetails = orderDetails.Where(od => combo.Items.Any(ci => ci.ProductId == od.ProductId));
                    var sumAffected = affectedDetails.Sum(d => d.UnitPrice * d.Quantity);
                    comboDiscount += (sumAffected * (combo.DiscountPercent.Value / 100m));
                }

                foreach (var item in combo.Items)
                {
                    if (item.ItemDiscountAmount.HasValue)
                    {
                        comboDiscount += item.ItemDiscountAmount.Value * maxApplicability;
                    }
                    if (item.ItemDiscountPercent.HasValue && item.ItemDiscountPercent.Value > 0)
                    {
                        var affected = orderDetails.Where(od => od.ProductId == item.ProductId);
                        var sumPrice = affected.Sum(d => d.UnitPrice * d.Quantity);
                        comboDiscount += sumPrice * (item.ItemDiscountPercent.Value / 100m);
                    }
                }

                if (comboDiscount < 0)
                {
                    comboDiscount = 0; // guard: non-negative discount
                }

                if (comboDiscount > 0)
                {
                    result.AppliedCombos.Add(new AppliedCombo
                    {
                        ComboId = combo.Id,
                        ComboName = combo.Name,
                        DiscountAmount = comboDiscount
                    });
                    result.DiscountTotal += comboDiscount;
                }
            }

            return result;
        }
    }
}


