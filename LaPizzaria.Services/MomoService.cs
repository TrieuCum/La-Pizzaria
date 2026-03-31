using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LaPizzaria.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LaPizzaria.Services;

/// <summary>
/// Triển khai gọi API MoMo và xác thực HMAC-SHA256.
/// </summary>
public class MomoService : IMomoService
{
    private readonly MomoOptionModel _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MomoService> _logger;

    public MomoService(
        IOptions<MomoOptionModel> options,
        IHttpClientFactory httpClientFactory,
        ILogger<MomoService> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Tạo chữ ký gửi lên MoMo khi request tạo payment (chuỗi raw theo tài liệu MoMo v2).
    /// </summary>
    private string BuildSignatureForCreateRequest(
        string accessKey,
        long amount,
        string extraData,
        string ipnUrl,
        string orderId,
        string orderInfo,
        string partnerCode,
        string redirectUrl,
        string requestId,
        string requestType)
    {
        var raw =
            $"accessKey={accessKey}" +
            $"&amount={amount}" +
            $"&extraData={extraData}" +
            $"&ipnUrl={ipnUrl}" +
            $"&orderId={orderId}" +
            $"&orderInfo={orderInfo}" +
            $"&partnerCode={partnerCode}" +
            $"&redirectUrl={redirectUrl}" +
            $"&requestId={requestId}" +
            $"&requestType={requestType}";
        return ComputeHmacSha256Hex(raw);
    }

    /// <summary>
    /// Tạo chữ ký để đối chiếu với callback MoMo (các field sắp xếp theo key alphabet).
    /// </summary>
    private string BuildSignatureForCallback(MomoExecuteResponseModel r)
    {
        var dict = new Dictionary<string, string>
        {
            ["accessKey"] = _options.AccessKey,
            ["amount"] = r.Amount.ToString(),
            ["extraData"] = r.ExtraData ?? string.Empty,
            ["message"] = r.Message ?? string.Empty,
            ["orderId"] = r.OrderId ?? string.Empty,
            ["orderInfo"] = r.OrderInfo ?? string.Empty,
            ["orderType"] = r.OrderType ?? string.Empty,
            ["partnerCode"] = r.PartnerCode ?? string.Empty,
            ["payType"] = r.PayType ?? string.Empty,
            ["requestId"] = r.RequestId ?? string.Empty,
            ["responseTime"] = r.ResponseTime.ToString(),
            ["resultCode"] = r.ResultCode.ToString(),
            ["transId"] = r.TransId.ToString()
        };

        var raw = string.Join("&", dict.OrderBy(x => x.Key).Select(kv => $"{kv.Key}={kv.Value}"));
        return ComputeHmacSha256Hex(raw, _options.SecretKey);
    }

    private string ComputeHmacSha256Hex(string rawData)
    {
        return ComputeHmacSha256Hex(rawData, _options.SecretKey);
    }

    private static string ComputeHmacSha256Hex(string rawData, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(rawData);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool VerifyCallbackSignature(MomoExecuteResponseModel response)
    {
        if (string.IsNullOrEmpty(response.Signature))
            return false;

        var expected = BuildSignatureForCallback(response);
        return string.Equals(expected, response.Signature, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<MomoCreatePaymentResponseModel?> CreatePaymentAsync(OrderInfoModel order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.PartnerCode) ||
            string.IsNullOrWhiteSpace(_options.AccessKey) ||
            string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            _logger.LogWarning("MoMo: thiếu PartnerCode, AccessKey hoặc SecretKey trong cấu hình.");
            return null;
        }

        var requestId = Guid.NewGuid().ToString("N");
        // MoMo yêu cầu orderId unique; ghép thêm timestamp để tránh trùng khi test lặp lại.
        var momoOrderId = $"{SanitizeOrderId(order.OrderId)}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        var amountLong = (long)Math.Round(order.Amount, 0, MidpointRounding.AwayFromZero);
        const string requestType = "payWithMethod";
        var extraData = order.ExtraData ?? string.Empty;
        var orderInfo = order.Name.Trim();

        var signature = BuildSignatureForCreateRequest(
            _options.AccessKey,
            amountLong,
            extraData,
            _options.NotifyUrl,
            momoOrderId,
            orderInfo,
            _options.PartnerCode,
            _options.ReturnUrl,
            requestId,
            requestType);

        var payload = new
        {
            partnerCode = _options.PartnerCode,
            partnerName = "LaPizzaria",
            storeId = "LaPizzariaStore",
            requestId,
            amount = amountLong,
            orderId = momoOrderId,
            orderInfo,
            redirectUrl = _options.ReturnUrl,
            ipnUrl = _options.NotifyUrl,
            lang = "vi",
            extraData,
            requestType,
            autoCapture = true,
            signature
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);

        try
        {
            using var httpResponse = await client.PostAsync(_options.MomoApiUrl, content, cancellationToken);
            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError("MoMo create failed: {Status} {Body}", httpResponse.StatusCode, body);
                return null;
            }

            var result = JsonSerializer.Deserialize<MomoCreatePaymentResponseModel>(body);
            if (result != null && result.ResultCode != 0)
            {
                _logger.LogWarning("MoMo resultCode={Code}: {Message}", result.ResultCode, result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API MoMo");
            return null;
        }
    }

    private static string SanitizeOrderId(string orderId)
    {
        var s = new string(orderId.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrEmpty(s) ? "ORDER" : s[..Math.Min(s.Length, 40)];
    }

    public MomoExecuteResponseModel ParseExecuteResponseFromQuery(IReadOnlyDictionary<string, string?> query)
    {
        static string? Q(IReadOnlyDictionary<string, string?> q, string key)
        {
            foreach (var kv in q)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
            return null;
        }

        long amount = 0;
        long transId = 0;
        long responseTime = 0;
        int resultCode = -1;

        _ = long.TryParse(Q(query, "amount"), out amount);
        _ = long.TryParse(Q(query, "transId"), out transId);
        _ = long.TryParse(Q(query, "responseTime"), out responseTime);
        _ = int.TryParse(Q(query, "resultCode"), out resultCode);

        return new MomoExecuteResponseModel
        {
            PartnerCode = Q(query, "partnerCode"),
            OrderId = Q(query, "orderId"),
            RequestId = Q(query, "requestId"),
            Amount = amount,
            OrderInfo = Q(query, "orderInfo"),
            OrderType = Q(query, "orderType"),
            TransId = transId,
            ResultCode = resultCode,
            Message = Q(query, "message"),
            PayType = Q(query, "payType"),
            ResponseTime = responseTime,
            ExtraData = Q(query, "extraData"),
            Signature = Q(query, "signature")
        };
    }
}
