using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInventoryService _inventory;
        private readonly IComboService _comboService;
        private readonly IPricingService _pricingService;

        public OrderService(ApplicationDbContext db, IInventoryService inventory, IComboService comboService, IPricingService pricingService)
        {
            _db = db;
            _inventory = inventory;
            _comboService = comboService;
            _pricingService = pricingService;
        }

        public async Task<Order> CreateOrderAsync(string? userId, IEnumerable<OrderDetail> items, IEnumerable<int> tableIds)
        {
            var itemList = items.ToList();
            var ok = await _inventory.CheckAndReserveAsync(itemList);
            if (!ok)
            {
                throw new System.InvalidOperationException("Insufficient ingredients");
            }

            var order = new Order
            {
                OrderStatus = "Pending",
                OrderDetails = itemList
            };
            if (!string.IsNullOrEmpty(userId))
            {
                // link to user by Id string
                order.User = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            }
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            if (tableIds != null)
            {
                await AssignTablesAsync(order.Id, tableIds);
            }

            order.TotalPrice = await CalculateTotalAsync(order.Id);
            await _db.SaveChangesAsync();
            return order;
        }

        public async Task<bool> MergeTablesAsync(int targetOrderId, IEnumerable<int> sourceTableIds)
        {
            var order = await _db.Orders.Include(o => o.OrderTables).FirstOrDefaultAsync(o => o.Id == targetOrderId);
            if (order == null) return false;

            // Enforce maximum 2 tables to merge at once
            var distinctTableIds = sourceTableIds?.Distinct().ToList() ?? new List<int>();
            if (distinctTableIds.Count == 0 || distinctTableIds.Count > 2) return false;

            var tables = await _db.Tables.Where(t => distinctTableIds.Contains(t.Id)).ToListAsync();
            foreach (var t in tables)
            {
                // Prevent assigning if table already attached to different active order
                var inUse = await _db.OrderTables.Include(ot => ot.Order)
                    .AnyAsync(ot => ot.TableId == t.Id && ot.OrderId != targetOrderId && ot.Order!.OrderStatus != "Completed");
                if (inUse) return false;

                // Skip if already attached to the target order to avoid duplicate tracking
                var alreadyAttached = order.OrderTables.Any(ot => ot.TableId == t.Id);
                if (!alreadyAttached)
                {
                    _db.OrderTables.Add(new OrderTable { OrderId = targetOrderId, TableId = t.Id });
                }
                t.IsOccupied = true;
            }
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<Order>> SplitOrderAsync(int orderId, Dictionary<int, int> orderDetailIdToNewQuantity)
        {
            var original = await _db.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == orderId);
            if (original == null) return new List<Order>();

            var newOrder = new Order { OrderStatus = "Pending" };
            _db.Orders.Add(newOrder);

            foreach (var kvp in orderDetailIdToNewQuantity)
            {
                var detail = original.OrderDetails.FirstOrDefault(d => d.Id == kvp.Key);
                if (detail == null) continue;
                var moveQty = kvp.Value;
                if (moveQty <= 0 || moveQty > detail.Quantity) continue;

                detail.Quantity -= moveQty;
                var moved = new OrderDetail
                {
                    ProductId = detail.ProductId,
                    Quantity = moveQty,
                    UnitPrice = detail.UnitPrice,
                    Subtotal = detail.UnitPrice * moveQty
                };
                newOrder.OrderDetails.Add(moved);
            }

            await _db.SaveChangesAsync();

            original.TotalPrice = await CalculateTotalAsync(original.Id);
            newOrder.TotalPrice = await CalculateTotalAsync(newOrder.Id);
            await _db.SaveChangesAsync();

            return new List<Order> { original, newOrder };
        }

        public async Task<decimal> CalculateTotalAsync(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.OrderVouchers).ThenInclude(ov => ov.Voucher)
                .FirstAsync(o => o.Id == orderId);
            // Apply dynamic pricing before calculating subtotals
            var productIds = order.OrderDetails.Select(x => x.ProductId).Distinct().ToList();
            var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
            foreach (var d in order.OrderDetails)
            {
                if (products.TryGetValue(d.ProductId, out var product))
                {
                    d.UnitPrice = _pricingService.AdjustUnitPrice(product, d.UnitPrice, System.DateTime.UtcNow);
                }
                d.Subtotal = d.UnitPrice * d.Quantity;
            }

            var subtotal = order.OrderDetails.Sum(d => d.Subtotal);

            var combos = await _db.Combos.Include(c => c.Items).ToListAsync();
            var suggestion = _comboService.SuggestBestCombos(order.OrderDetails, combos);
            var discount = suggestion.DiscountTotal;
            if (discount < 0) discount = 0; // guard non-negative

            // voucher discounts (up to 2 vouchers already attached to order)
            decimal voucherDiscount = 0m;
            var now = System.DateTime.UtcNow;
            if (order.OrderVouchers != null)
            {
                foreach (var ov in order.OrderVouchers.Take(2))
                {
                    var v = ov.Voucher;
                    if (v == null) continue;
                    if (v.IsActive && (v.ExpiresAtUtc == null || v.ExpiresAtUtc > now) && (v.MaxUses == 0 || v.UsedCount <= v.MaxUses))
                    {
                        voucherDiscount += System.Math.Round(subtotal * (v.DiscountPercent / 100m), 2);
                    }
                }
            }

            var total = subtotal - discount - voucherDiscount;
            if (total < 0) total = 0;
            return total;
        }

        public async Task<bool> AssignTablesAsync(int orderId, IEnumerable<int> tableIds)
        {
            var order = await _db.Orders.Include(o => o.OrderTables).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return false;

            foreach (var tid in tableIds)
            {
                var table = await _db.Tables.FindAsync(tid);
                if (table == null) continue;

                var inUse = await _db.OrderTables.Include(ot => ot.Order)
                    .AnyAsync(ot => ot.TableId == tid && ot.Order!.OrderStatus != "Completed");
                if (inUse) return false;

                _db.OrderTables.Add(new OrderTable { OrderId = orderId, TableId = tid });
                table.IsOccupied = true;
            }
            await _db.SaveChangesAsync();
            return true;
        }
    }
}


