using LaPizzaria.Models;
using LaPizzaria.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LaPizzaria.Controllers;

/// <summary>
/// Thanh toán MoMo (sandbox): tạo giao dịch và nhận callback sau khi người dùng thanh toán.
/// </summary>
[AllowAnonymous]
public class PaymentController : Controller
{
    private readonly IMomoService _momoService;
    private readonly IMemoryCache _cache;
    private readonly IOrderPlacementService _orderPlacement;

    public PaymentController(IMomoService momoService, IMemoryCache cache, IOrderPlacementService orderPlacement)
    {
        _momoService = momoService;
        _cache = cache;
        _orderPlacement = orderPlacement;
    }

    /// <summary>
    /// Trang demo nhập thông tin đơn hàng (OrderInfoModel) và gửi POST tạo thanh toán.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View(new OrderInfoModel());
    }

    /// <summary>
    /// Bắt đầu giao dịch: gọi MoMo API, nếu thành công chuyển hướng người dùng tới <c>payUrl</c>.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayment(OrderInfoModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("Index", model);

        var apiResult = await _momoService.CreatePaymentAsync(model, cancellationToken);
        if (apiResult == null || apiResult.ResultCode != 0 || string.IsNullOrWhiteSpace(apiResult.PayUrl))
        {
            ViewBag.ErrorMessage = apiResult?.Message ?? "Không tạo được giao dịch MoMo. Kiểm tra cấu hình và kết nối.";
            return View("Index", model);
        }

        // Chuyển sang trang thanh toán test của MoMo (sandbox).
        return Redirect(apiResult.PayUrl);
    }

    /// <summary>
    /// MoMo redirect (GET) sau khi người dùng thanh toán — kiểm tra chữ ký; nếu thành công thì tạo đơn rồi chuyển trang hoàn tất.
    /// URL phải khớp <c>Momo:ReturnUrl</c> trong appsettings.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PaymentCallback()
    {
        try
        {
            var queryDict = Request.Query.Keys.ToDictionary(
                k => k,
                k => (string?)Request.Query[k].ToString(),
                StringComparer.OrdinalIgnoreCase);

            var model = _momoService.ParseExecuteResponseFromQuery(queryDict);
            model.SignatureValid = _momoService.VerifyCallbackSignature(model);

            if (!model.SignatureValid)
                return View("Result", model);

            if (!model.IsSuccess)
                return View("Result", model);

            // Thanh toán thử từ Payment/Index (không gắn giỏ hàng) — không có extraData.
            if (string.IsNullOrWhiteSpace(model.ExtraData))
                return View("Result", model);

            if (!Guid.TryParse(model.ExtraData, out var pendingId))
            {
                TempData["PaymentError"] = "Mã phiên thanh toán (extraData) không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var cacheKey = $"momo_pending_{pendingId}";
            if (!_cache.TryGetValue(cacheKey, out PendingCheckoutState? pending) || pending == null)
            {
                TempData["PaymentError"] = "Phiên thanh toán hết hạn hoặc không tồn tại. Nếu đã trừ tiền, vui lòng liên hệ hỗ trợ kèm mã giao dịch MoMo.";
                return RedirectToAction(nameof(Index));
            }

            if (pending.ExpectedAmountVnd != model.Amount)
            {
                _cache.Remove(cacheKey);
                TempData["PaymentError"] = "Số tiền thanh toán không khớp với đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var orderId = await _orderPlacement.PlaceQrOrderAsync(pending.Request, "MoMo");
                _cache.Remove(cacheKey);
                return RedirectToAction(nameof(CheckoutComplete), new { orderId });
            }
            catch (InvalidOperationException ex)
            {
                _cache.Remove(cacheKey);
                TempData["PaymentError"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
        catch (Exception ex)
        {
            TempData["PaymentError"] = $"Không xử lý được kết quả thanh toán: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>Sau khi MoMo thành công và đơn đã được tạo.</summary>
    [HttpGet]
    public IActionResult CheckoutComplete(int orderId)
    {
        ViewBag.OrderId = orderId;
        return View();
    }

    /// <summary>
    /// IPN server-to-server (POST). MoMo gọi tới <c>Momo:NotifyUrl</c> — xác thực chữ ký và trả 200.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult PaymentNotify([FromBody] MomoExecuteResponseModel? body)
    {
        if (body == null)
            return BadRequest();

        body.SignatureValid = _momoService.VerifyCallbackSignature(body);
        if (!body.SignatureValid)
            return BadRequest(new { message = "Chữ ký IPN không hợp lệ." });

        // TODO: cập nhật trạng thái đơn hàng trong DB theo orderId / extraData khi tích hợp nghiệp vụ.
        return Ok(new { message = "Received" });
    }
}
