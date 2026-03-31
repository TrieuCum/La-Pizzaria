namespace LaPizzaria.Models;

public sealed class CheckoutTotalsResult
{
    public decimal Subtotal { get; set; }
    public decimal VoucherDiscount { get; set; }
    public decimal ShipFee { get; set; }
    public decimal Vat { get; set; }
    public decimal GrandTotal { get; set; }
    public long AmountVnd { get; set; }
}

public sealed class CheckoutTotalsOutcome
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public CheckoutTotalsResult? Totals { get; init; }

    public static CheckoutTotalsOutcome Ok(CheckoutTotalsResult t) =>
        new() { Success = true, Totals = t };

    public static CheckoutTotalsOutcome Fail(string err) =>
        new() { Success = false, Error = err };
}

/// <summary>Trạng thái chờ thanh toán MoMo (IMemoryCache).</summary>
public sealed class PendingCheckoutState
{
    public QrOrderRequest Request { get; set; } = null!;
    public long ExpectedAmountVnd { get; set; }
}
