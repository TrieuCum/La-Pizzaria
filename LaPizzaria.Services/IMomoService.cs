using LaPizzaria.Models;

namespace LaPizzaria.Services;

/// <summary>
/// Dịch vụ tích hợp cổng thanh toán MoMo (sandbox/production).
/// </summary>
public interface IMomoService
{
    /// <summary>
    /// Tạo giao dịch và gọi API MoMo để lấy <c>payUrl</c>.
    /// </summary>
    Task<MomoCreatePaymentResponseModel?> CreatePaymentAsync(OrderInfoModel order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra chữ ký callback từ MoMo (redirect sau thanh toán).
    /// </summary>
    bool VerifyCallbackSignature(MomoExecuteResponseModel response);

    /// <summary>
    /// Đọc tham số query MoMo trả về (ReturnUrl GET). Truyền từ <c>Request.Query</c> đã chuyển sang dictionary.
    /// </summary>
    MomoExecuteResponseModel ParseExecuteResponseFromQuery(IReadOnlyDictionary<string, string?> query);
}
