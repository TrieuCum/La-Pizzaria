using LaPizzaria.Models;

namespace LaPizzaria.Helpers;

public static class OrderTrackingHelper
{
    /// <summary>Bản đồ tracking khi đã có shipper và đơn ở assigned/delivering, chưa hoàn thành/hủy.</summary>
    public static bool CanShowShipperMap(Order o)
    {
        if (string.IsNullOrWhiteSpace(o.ShipperId)) return false;
        if (string.IsNullOrWhiteSpace(o.DeliveryAddress)) return false;
        if (string.Equals(o.OrderStatus, "Completed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(o.OrderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return false;

        var ds = o.DeliveryStatus ?? string.Empty;
        return string.Equals(ds, "assigned", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ds, "delivering", StringComparison.OrdinalIgnoreCase);
    }
}
