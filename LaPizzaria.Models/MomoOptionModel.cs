namespace LaPizzaria.Models;

/// <summary>
/// Cấu hình cổng thanh toán MoMo (bind từ appsettings section "Momo").
/// </summary>
public class MomoOptionModel
{
    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>URL API tạo giao dịch (sandbox: test-payment.momo.vn).</summary>
    public string MomoApiUrl { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";

    /// <summary>URL MoMo redirect người dùng sau khi thanh toán (GET).</summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>URL IPN MoMo gọi server-to-server (POST).</summary>
    public string NotifyUrl { get; set; } = string.Empty;
}
