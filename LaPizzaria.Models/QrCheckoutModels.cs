namespace LaPizzaria.Models;

/// <summary>Payload đặt hàng từ giỏ (QR / Card).</summary>
public class QrOrderRequest
{
    public string? TableCode { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? UserId { get; set; }
    public List<QrOrderItem>? Items { get; set; }
    public List<int>? VoucherIds { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>ship | dine — khớp JS trên trang Card.</summary>
    public string? DeliveryType { get; set; }

    /// <summary>Khoảng cách tính phí ship (mét); &gt; 0 mới tính phí.</summary>
    public double? TravelDistanceMeters { get; set; }
}

public class QrOrderItem
{
    public int ProductId { get; set; }
    public int? ProductId2 { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Size { get; set; }
}
