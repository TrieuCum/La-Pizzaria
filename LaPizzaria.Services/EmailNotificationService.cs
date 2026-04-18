using System.Net;
using System.Net.Mail;
using LaPizzaria.Data;
using LaPizzaria.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LaPizzaria.Services;

public class EmailNotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public EmailNotificationService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<(int sent, string? error)> SendNewProductNotificationAsync(Product product, string baseUrl)
    {
        var subject = $"🍕 LaPizzaria vừa ra mắt món mới: {product.Name}!";
        var body = BuildProductEmailHtml(product, baseUrl);
        return await BroadcastToAllCustomersAsync(subject, body);
    }

    public async Task<(int sent, string? error)> SendNewVoucherNotificationAsync(Voucher voucher, string baseUrl)
    {
        var subject = $"🎉 LaPizzaria có ưu đãi mới: {voucher.Name}!";
        var body = BuildVoucherEmailHtml(voucher, baseUrl);
        return await BroadcastToAllCustomersAsync(subject, body);
    }

    private async Task<(int sent, string? error)> BroadcastToAllCustomersAsync(string subject, string htmlBody)
    {
        var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.TryParse(_config["Email:SmtpPort"], out var p) ? p : 587;
        var enableSsl = bool.TryParse(_config["Email:EnableSsl"], out var ssl) ? ssl : true;
        var senderEmail = _config["Email:SenderEmail"] ?? "";
        var senderName = _config["Email:SenderName"] ?? "LaPizzaria";
        var username = _config["Email:Username"] ?? senderEmail;
        var password = _config["Email:Password"] ?? "";

        if (string.IsNullOrWhiteSpace(password))
            return (0, "Chưa cấu hình mật khẩu email SMTP trong appsettings.json (Email:Password).");

        // Exclude staff accounts (Admin / Staff / Shipper); everyone else is a customer
        var staffRoleIds = await _db.Roles
            .Where(r => r.Name == "Admin" || r.Name == "Staff" || r.Name == "Shipper")
            .Select(r => r.Id)
            .ToListAsync();

        var staffUserIds = staffRoleIds.Any()
            ? await _db.UserRoles
                .Where(ur => staffRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync()
            : new List<string>();

        var emails = await _db.Users
            .Where(u => u.Email != null && u.IsActive && !staffUserIds.Contains(u.Id))
            .Select(u => u.Email!)
            .Distinct()
            .ToListAsync();

        if (!emails.Any()) return (0, "Không có khách hàng nào đang hoạt động để gửi email.");

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

            int sent = 0;
            foreach (var email in emails)
            {
                try
                {
                    var msg = new MailMessage
                    {
                        From = new MailAddress(senderEmail, senderName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };
                    msg.To.Add(email);
                    await client.SendMailAsync(msg);
                    sent++;
                }
                catch
                {
                    // Skip individual failures silently
                }
            }
            return (sent, null);
        }
        catch (Exception ex)
        {
            return (0, $"Lỗi kết nối SMTP: {ex.Message}");
        }
    }

    private static string BuildProductEmailHtml(Product product, string baseUrl)
    {
        var imageUrl = string.IsNullOrWhiteSpace(product.ImageUrl) ? "" : product.ImageUrl;
        var menuUrl = $"{baseUrl.TrimEnd('/')}/Menu";
        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='margin:0;padding:0;background:#f4f4f4;font-family:Arial,sans-serif;'>
  <div style='max-width:600px;margin:40px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
    <div style='background:#f97316;padding:28px 32px;text-align:center;'>
      <div style='color:#fff;font-size:32px;font-weight:900;letter-spacing:-1px;'>🍕 LaPizzaria</div>
      <div style='color:#ffe0c0;font-size:14px;margin-top:6px;'>Món mới toanh đã sẵn sàng!</div>
    </div>
    {(string.IsNullOrEmpty(imageUrl) ? "" : $"<img src='{imageUrl}' style='width:100%;max-height:300px;object-fit:cover;'/>")}
    <div style='padding:32px;'>
      <h1 style='color:#1a1a1a;font-size:24px;margin:0 0 12px;'>{product.Name}</h1>
      <p style='color:#555;font-size:14px;line-height:1.6;'>{product.Description ?? ""}</p>
      <div style='margin:20px 0;font-size:22px;font-weight:900;color:#f97316;'>{product.Price:N0}đ</div>
      <p style='color:#555;font-size:14px;'>Hãy vào <strong>LaPizzaria</strong> và đặt món ngay để thưởng thức sớm nhất nhé!</p>
      <div style='text-align:center;margin-top:28px;'>
        <a href='{menuUrl}' style='background:#f97316;color:#fff;text-decoration:none;padding:14px 32px;border-radius:8px;font-weight:700;font-size:15px;display:inline-block;'>Đặt món ngay →</a>
      </div>
    </div>
    <div style='background:#f8f7f5;padding:16px 32px;text-align:center;font-size:11px;color:#aaa;'>
      © LaPizzaria — Bạn nhận được email này vì đã đăng ký tài khoản tại LaPizzaria.
    </div>
  </div>
</body>
</html>";
    }

    private static string BuildVoucherEmailHtml(Voucher voucher, string baseUrl)
    {
        var discountText = voucher.VoucherType == "Percentage"
            ? $"Giảm {voucher.DiscountPercent}%"
            : voucher.VoucherType == "FreeShipping"
                ? "Miễn phí giao hàng"
                : $"Giảm {voucher.DiscountAmount:N0}đ";
        var menuUrl = $"{baseUrl.TrimEnd('/')}/Menu";
        var expiry = voucher.ExpiresAtUtc.HasValue
            ? $"HSD: {voucher.ExpiresAtUtc.Value.ToLocalTime():dd/MM/yyyy}"
            : "Không giới hạn thời gian";

        return $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'/></head>
<body style='margin:0;padding:0;background:#f4f4f4;font-family:Arial,sans-serif;'>
  <div style='max-width:600px;margin:40px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>
    <div style='background:#f97316;padding:28px 32px;text-align:center;'>
      <div style='color:#fff;font-size:32px;font-weight:900;letter-spacing:-1px;'>🍕 LaPizzaria</div>
      <div style='color:#ffe0c0;font-size:14px;margin-top:6px;'>Ưu đãi mới dành cho bạn!</div>
    </div>
    <div style='padding:32px;'>
      <div style='background:#fff7ed;border:2px dashed #f97316;border-radius:12px;padding:24px;text-align:center;margin-bottom:24px;'>
        <div style='font-size:12px;font-weight:700;color:#f97316;text-transform:uppercase;letter-spacing:1px;margin-bottom:8px;'>Mã giảm giá</div>
        <div style='font-size:28px;font-weight:900;color:#1a1a1a;letter-spacing:2px;'>{voucher.Code}</div>
        <div style='font-size:20px;font-weight:700;color:#f97316;margin-top:8px;'>{discountText}</div>
        <div style='font-size:12px;color:#aaa;margin-top:6px;'>{expiry}</div>
      </div>
      <h2 style='color:#1a1a1a;font-size:20px;'>{voucher.Name}</h2>
      {(voucher.MinOrderValue > 0 ? $"<p style='color:#555;font-size:13px;'>Đơn tối thiểu: <strong>{voucher.MinOrderValue:N0}đ</strong></p>" : "")}
      <div style='text-align:center;margin-top:24px;'>
        <a href='{menuUrl}' style='background:#f97316;color:#fff;text-decoration:none;padding:14px 32px;border-radius:8px;font-weight:700;font-size:15px;display:inline-block;'>Đặt món ngay →</a>
      </div>
    </div>
    <div style='background:#f8f7f5;padding:16px 32px;text-align:center;font-size:11px;color:#aaa;'>
      © LaPizzaria — Bạn nhận được email này vì đã đăng ký tài khoản tại LaPizzaria.
    </div>
  </div>
</body>
</html>";
    }
}
