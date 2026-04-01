using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.EntityFrameworkCore;

namespace LaPizzaria.Services;

public sealed class OrderPlacementService : IOrderPlacementService
{
    private readonly ApplicationDbContext _db;
    private readonly IOrderService _orderService;
    private readonly IVoucherService _voucherService;

    public OrderPlacementService(ApplicationDbContext db, IOrderService orderService, IVoucherService voucherService)
    {
        _db = db;
        _orderService = orderService;
        _voucherService = voucherService;
    }

    public async Task<CheckoutTotalsOutcome> ComputeTotalsAsync(QrOrderRequest req)
    {
        var items = req.Items ?? new List<QrOrderItem>();
        var productIds = items.Select(i => i.ProductId).Concat(items.Where(i => i.ProductId2.HasValue).Select(i => i.ProductId2!.Value)).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var details = new List<OrderDetail>();
        foreach (var it in items)
        {
            if (!products.TryGetValue(it.ProductId, out var p1) && it.UnitPrice == null)
                continue;

            decimal price = 0;
            if (it.UnitPrice != null)
                price = it.UnitPrice.Value;
            else if (p1 != null)
            {
                decimal p1Price = p1.Price;
                decimal modifier = 0;
                if (it.Size == "S") modifier = -30000;
                else if (it.Size == "L") modifier = 50000;

                decimal finalUnitPrice = p1Price + modifier;

                if (it.ProductId2.HasValue && products.TryGetValue(it.ProductId2.Value, out var p2))
                {
                    decimal p2Price = p2.Price + modifier;
                    finalUnitPrice = (finalUnitPrice + p2Price) / 2;
                }
                price = finalUnitPrice;
            }

            details.Add(new OrderDetail { ProductId = it.ProductId, ProductId2 = it.ProductId2, Quantity = it.Quantity, UnitPrice = price, Subtotal = price * it.Quantity, Size = it.Size });
        }

        var subtotal = details.Sum(d => d.Subtotal);
        decimal voucherDiscount = 0m;

        foreach (var vid in req.VoucherIds?.Take(2) ?? new List<int>())
        {
            var v = await _voucherService.GetByIdAsync(vid);
            if (v == null) continue;

            if (!_voucherService.IsUsable(v, DateTime.Now, details))
                return CheckoutTotalsOutcome.Fail($"Voucher {v.Code} hiện không khả dụng.");

            if (subtotal < v.MinOrderValue)
                return CheckoutTotalsOutcome.Fail($"Đơn hàng chưa đạt giá trị tối thiểu ({v.MinOrderValue:N0}đ) để sử dụng mã {v.Code}.");

            if (v.VoucherType == "Percentage")
                voucherDiscount += Math.Round(subtotal * (v.DiscountPercent / 100m), 2);
            else if (v.VoucherType == "FixedAmount")
                voucherDiscount += v.DiscountAmount;
            else if (v.VoucherType == "FreeShipping")
                voucherDiscount += v.DiscountAmount;
        }

        var deliveryType = string.IsNullOrWhiteSpace(req.DeliveryType) ? "ship" : req.DeliveryType;
        decimal shipFee = 0;
        if (string.Equals(deliveryType, "ship", StringComparison.OrdinalIgnoreCase) &&
            req.TravelDistanceMeters.HasValue &&
            req.TravelDistanceMeters.Value > 0)
        {
            var distKm = req.TravelDistanceMeters.Value / 1000.0;
            shipFee = distKm <= 2 ? 15000m : 25000m;
        }

        var vat = Math.Round((subtotal - voucherDiscount) * 0.05m, 0, MidpointRounding.AwayFromZero);
        var grandTotal = Math.Max(0, subtotal + shipFee - voucherDiscount + vat);
        var amountVnd = (long)Math.Round(grandTotal, 0, MidpointRounding.AwayFromZero);

        if (amountVnd < 1)
            return CheckoutTotalsOutcome.Fail("Số tiền thanh toán không hợp lệ.");

        return CheckoutTotalsOutcome.Ok(new CheckoutTotalsResult
        {
            Subtotal = subtotal,
            VoucherDiscount = voucherDiscount,
            ShipFee = shipFee,
            Vat = vat,
            GrandTotal = grandTotal,
            AmountVnd = amountVnd
        });
    }

    public async Task<int> PlaceQrOrderAsync(QrOrderRequest req, string? paymentMethod = null)
    {
        var details = new List<OrderDetail>();
        if (req.Items != null)
        {
            var productIds = req.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            foreach (var it in req.Items)
            {
                decimal price = it.UnitPrice ?? 0m;
                if (it.UnitPrice == null && products.TryGetValue(it.ProductId, out var p1))
                {
                    decimal p1Price = p1.Price;
                    decimal modifier = 0;
                    if (it.Size == "S") modifier = -30000;
                    else if (it.Size == "L") modifier = 50000;

                    decimal finalUnitPrice = p1Price + modifier;

                    if (it.ProductId2.HasValue && products.TryGetValue(it.ProductId2.Value, out var p2))
                    {
                        decimal p2Price = p2.Price + modifier;
                        finalUnitPrice = (finalUnitPrice + p2Price) / 2;
                    }
                    price = finalUnitPrice;
                }

                details.Add(new OrderDetail
                {
                    ProductId = it.ProductId,
                    ProductId2 = it.ProductId2,
                    Quantity = it.Quantity,
                    UnitPrice = price,
                    Subtotal = price * it.Quantity,
                    Size = it.Size
                });
            }
        }

        var tableIds = new List<int>();
        if (!string.IsNullOrWhiteSpace(req.TableCode))
        {
            var t = await _db.Tables.FirstOrDefaultAsync(x => x.Code == req.TableCode);
            if (t != null) tableIds.Add(t.Id);
        }

        var order = await _orderService.CreateOrderAsync(req.UserId, details, tableIds, req.DeliveryAddress, req.Latitude, req.Longitude);

        if (req.VoucherIds != null && req.VoucherIds.Count > 0)
        {
            foreach (var vid in req.VoucherIds.Take(2))
            {
                var v = await _voucherService.GetByIdAsync(vid);
                if (v != null && _voucherService.IsUsable(v, DateTime.Now, details))
                {
                    _db.OrderVouchers.Add(new OrderVoucher { OrderId = order.Id, VoucherId = v.Id });
                    v.UsedCount += 1;
                }
            }
            await _db.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            order.PaymentMethod = paymentMethod;
            await _db.SaveChangesAsync();
        }

        return order.Id;
    }
}
