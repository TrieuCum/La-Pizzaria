namespace LaPizzaria.ViewModels;

/// <summary>Partial bản đồ shipper trên trang chi tiết đơn (khách / quản lý).</summary>
public class OrderShipperTrackingPartialModel
{
    public int OrderId { get; set; }
    public bool ShowMap { get; set; }
    public string? GoogleMapsApiKey { get; set; }
    /// <summary>CSS bổ sung cho wrapper (vd. admin vs khách).</summary>
    public string WrapperClass { get; set; } = "";
}
