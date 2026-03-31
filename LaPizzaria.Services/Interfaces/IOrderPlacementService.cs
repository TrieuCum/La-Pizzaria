using LaPizzaria.Models;

namespace LaPizzaria.Services;

public interface IOrderPlacementService
{
    /// <summary>Tính tổng tiền (subtotal, ship, VAT) khớp Preview + Card JS.</summary>
    Task<CheckoutTotalsOutcome> ComputeTotalsAsync(QrOrderRequest req);

    /// <summary>Tạo đơn như <c>FromQr</c>; gán <paramref name="paymentMethod"/> nếu có.</summary>
    Task<int> PlaceQrOrderAsync(QrOrderRequest req, string? paymentMethod = null);
}
