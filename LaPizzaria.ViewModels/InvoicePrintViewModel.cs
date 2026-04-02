using System.Collections.Generic;
using LaPizzaria.Models;

namespace LaPizzaria.ViewModels
{
    /// <summary>Dữ liệu in hóa đơn theo mẫu LP/26E.</summary>
    public class InvoicePrintViewModel
    {
        public Order Order { get; set; } = null!;

        /// <summary>Ký hiệu hóa đơn, ví dụ: LP/26E</summary>
        public string InvoiceSymbol { get; set; } = "LP/26E";

        /// <summary>Số hóa đơn, ví dụ: 000017</summary>
        public string InvoiceNumber { get; set; } = "";

        /// <summary>Ngày lập hóa đơn (dd/MM/yyyy HH:mm)</summary>
        public string IssueDate { get; set; } = "";

        // Người bán (có thể lấy từ config)
        public string SellerName { get; set; } = "Công ty TNHH LaPizzaria";
        public string SellerTaxCode { get; set; } = "0312345678";
        public string SellerAddress { get; set; } = "193 Đỗ Văn Thi, Phường, Biên Hòa, Đồng Nai";
        public string SellerPhone { get; set; } = "0901 234 567";
        public string SellerEmail { get; set; } = "support@lapizzaria.vn";

        // Người mua (từ Order + User)
        public string BuyerName { get; set; } = "";
        public string BuyerPhone { get; set; } = "";
        public string BuyerAddress { get; set; } = "";

        // Số tiền
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal VoucherDiscount { get; set; }
        public decimal TotalBeforeTax { get; set; }
        public decimal TaxPercent { get; set; } = 10;
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }

        /// <summary>Bằng chữ (tiếng Việt)</summary>
        public string AmountInWords { get; set; } = "";

        public string PaymentMethodDisplay { get; set; } = "Chưa chọn";
        public string PaymentStatus { get; set; } = "Chưa thanh toán";
        public string OrderCode { get; set; } = "";
        public string OrderDateDisplay { get; set; } = "";
        public string OrderStatusDisplay { get; set; } = "";

        /// <summary>Chi tiết dòng in (tên, SL, đơn giá, thành tiền)</summary>
        public List<InvoicePrintLine> Lines { get; set; } = new List<InvoicePrintLine>();
    }

    public class InvoicePrintLine
    {
        public int Stt { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }
}
