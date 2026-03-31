using System.Text.Json.Serialization;

namespace LaPizzaria.Models;

/// <summary>
/// Kết quả sau khi người dùng hoàn tất thanh toán (redirect về ReturnUrl hoặc IPN).
/// </summary>
public class MomoExecuteResponseModel
{
    [JsonPropertyName("partnerCode")]
    public string? PartnerCode { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("orderInfo")]
    public string? OrderInfo { get; set; }

    [JsonPropertyName("orderType")]
    public string? OrderType { get; set; }

    [JsonPropertyName("transId")]
    public long TransId { get; set; }

    [JsonPropertyName("resultCode")]
    public int ResultCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("payType")]
    public string? PayType { get; set; }

    [JsonPropertyName("responseTime")]
    public long ResponseTime { get; set; }

    [JsonPropertyName("extraData")]
    public string? ExtraData { get; set; }

    [JsonPropertyName("signature")]
    public string? Signature { get; set; }

    /// <summary>Chữ ký hợp lệ theo SecretKey hay không.</summary>
    public bool SignatureValid { get; set; }

    /// <summary>Thanh toán thành công (MoMo: resultCode == 0).</summary>
    public bool IsSuccess => ResultCode == 0;
}
