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
            var productIds = orderDetails.Select(od => od.ProductId).Distinct().ToList();
            var productIngredients = await _db.ProductIngredients
                .Where(pi => productIds.Contains(pi.ProductId))
                .ToListAsync();

            var ingredientNeeds = new Dictionary<int, decimal>();
            foreach (var od in orderDetails)
            {
                foreach (var pi in productIngredients.Where(p => p.ProductId == od.ProductId))
                {
                    var need = pi.QuantityPerUnit * od.Quantity;
                    if (!ingredientNeeds.ContainsKey(pi.IngredientId)) ingredientNeeds[pi.IngredientId] = 0;
                    ingredientNeeds[pi.IngredientId] += need;
                }
            }

            var ingredients = await _db.Ingredients.Where(i => ingredientNeeds.Keys.Contains(i.Id)).ToListAsync();
            foreach (var ing in ingredients)
            {
                if (ing.StockQuantity < ingredientNeeds[ing.Id])
                {
                    return false;
                }
            }

            // Reserve by deducting temporarily (simplified; in real life, use reservation records/locking)
            foreach (var ing in ingredients)
            {
                ing.StockQuantity -= ingredientNeeds[ing.Id];
            }
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task ReleaseAsync(IEnumerable<OrderDetail> orderDetails)
        {
            var productIds = orderDetails.Select(od => od.ProductId).Distinct().ToList();
            var productIngredients = await _db.ProductIngredients
                .Where(pi => productIds.Contains(pi.ProductId))
                .ToListAsync();

            var ingredientReleases = new Dictionary<int, decimal>();
            foreach (var od in orderDetails)
            {
                foreach (var pi in productIngredients.Where(p => p.ProductId == od.ProductId))
                {
                    var qty = pi.QuantityPerUnit * od.Quantity;
                    if (!ingredientReleases.ContainsKey(pi.IngredientId)) ingredientReleases[pi.IngredientId] = 0;
                    ingredientReleases[pi.IngredientId] += qty;
                }
            }

            var ingredients = await _db.Ingredients.Where(i => ingredientReleases.Keys.Contains(i.Id)).ToListAsync();
            foreach (var ing in ingredients)
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


