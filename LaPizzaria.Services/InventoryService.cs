using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _db;

        public InventoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> CheckAndReserveAsync(IEnumerable<OrderDetail> orderDetails)
        {
            var productIds = orderDetails
                .Select(od => od.ProductId)
                .Concat(orderDetails.Where(od => od.ProductId2.HasValue).Select(od => od.ProductId2!.Value))
                .Distinct()
                .ToList();
            var productIngredients = await _db.ProductIngredients
                .Where(pi => productIds.Contains(pi.ProductId))
                .ToListAsync();
            var ingredientIds = productIngredients.Select(pi => pi.IngredientId).Distinct().ToList();
            var ingredients = await _db.Ingredients.Where(i => ingredientIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id);

            var ingredientNeeds = new Dictionary<int, decimal>();
            foreach (var od in orderDetails)
            {
                foreach (var pi in productIngredients.Where(p => p.ProductId == od.ProductId))
                {
                    if (!ingredients.TryGetValue(pi.IngredientId, out var ingredient)) continue;
                    var need = ProductPricingCalculator.GetScaledQuantity(pi, ingredient, od.Size) * od.Quantity;
                    if (!ingredientNeeds.ContainsKey(pi.IngredientId)) ingredientNeeds[pi.IngredientId] = 0;
                    ingredientNeeds[pi.IngredientId] += need;
                }

                if (od.ProductId2.HasValue)
                {
                    foreach (var pi in productIngredients.Where(p => p.ProductId == od.ProductId2.Value))
                    {
                        if (!ingredients.TryGetValue(pi.IngredientId, out var ingredient)) continue;
                        var need = ProductPricingCalculator.GetScaledQuantity(pi, ingredient, od.Size) * od.Quantity * 0.5m;
                        if (!ingredientNeeds.ContainsKey(pi.IngredientId)) ingredientNeeds[pi.IngredientId] = 0;
                        ingredientNeeds[pi.IngredientId] += need;
                    }
                }
            }

            var targetIngredients = await _db.Ingredients.Where(i => ingredientNeeds.Keys.Contains(i.Id)).ToListAsync();
            foreach (var ing in targetIngredients)
            {
                if (ing.StockQuantity < ingredientNeeds[ing.Id])
                {
                    return false;
                }
            }

            // Reserve by deducting temporarily (simplified; in real life, use reservation records/locking)
            foreach (var ing in targetIngredients)
            {
                ing.StockQuantity -= ingredientNeeds[ing.Id];
            }
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task ReleaseAsync(IEnumerable<OrderDetail> orderDetails)
        {
            var productIds = orderDetails
                .Select(od => od.ProductId)
                .Concat(orderDetails.Where(od => od.ProductId2.HasValue).Select(od => od.ProductId2!.Value))
                .Distinct()
                .ToList();
            var productIngredients = await _db.ProductIngredients
                .Where(pi => productIds.Contains(pi.ProductId))
                .ToListAsync();
            var ingredientIds = productIngredients.Select(pi => pi.IngredientId).Distinct().ToList();
            var ingredients = await _db.Ingredients.Where(i => ingredientIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id);

            var ingredientReleases = new Dictionary<int, decimal>();
            foreach (var od in orderDetails)
            {
                foreach (var pi in productIngredients.Where(p => p.ProductId == od.ProductId))
                {
                    if (!ingredients.TryGetValue(pi.IngredientId, out var ingredient)) continue;
                    var qty = ProductPricingCalculator.GetScaledQuantity(pi, ingredient, od.Size) * od.Quantity;
                    if (!ingredientReleases.ContainsKey(pi.IngredientId)) ingredientReleases[pi.IngredientId] = 0;
                    ingredientReleases[pi.IngredientId] += qty;
                }

                if (od.ProductId2.HasValue)
                {
                    foreach (var pi in productIngredients.Where(p => p.ProductId == od.ProductId2.Value))
                    {
                        if (!ingredients.TryGetValue(pi.IngredientId, out var ingredient)) continue;
                        var qty = ProductPricingCalculator.GetScaledQuantity(pi, ingredient, od.Size) * od.Quantity * 0.5m;
                        if (!ingredientReleases.ContainsKey(pi.IngredientId)) ingredientReleases[pi.IngredientId] = 0;
                        ingredientReleases[pi.IngredientId] += qty;
                    }
                }
            }

            var releasedIngredients = await _db.Ingredients.Where(i => ingredientReleases.Keys.Contains(i.Id)).ToListAsync();
            foreach (var ing in releasedIngredients)
            {
                ing.StockQuantity += ingredientReleases[ing.Id];
            }
            await _db.SaveChangesAsync();
        }

        public Task<bool> ConsumeAsync(IEnumerable<OrderDetail> orderDetails)
        {
            // In this simplified approach, reservation already deducted stock, so nothing to do.
            return Task.FromResult(true);
        }
    }
}


