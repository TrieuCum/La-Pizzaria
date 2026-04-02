namespace LaPizzaria.Models
{
    /// <summary>Voucher mà user đã bấm "Lưu mã" — lưu vào tài khoản.</summary>
    public class UserSavedVoucher
    {
        public string UserId { get; set; } = string.Empty;
        public int VoucherId { get; set; }
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual ApplicationUser? User { get; set; }
        public virtual Voucher? Voucher { get; set; }
    }
}
