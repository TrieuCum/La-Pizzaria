namespace LaPizzaria.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Đã nhận đơn"; // Đặt hàng
        public const string Preparing = "Đang chế biến"; // Đầu bếp đang chuẩn bị
        public const string Delivering = "Đang giao"; // Đơn hàng sẽ sớm được vận chuyển
        public const string Completed = "Hoàn thành"; // Chúc bạn ngon miệng
        public const string Cancelled = "Đã hủy";
    }
}
