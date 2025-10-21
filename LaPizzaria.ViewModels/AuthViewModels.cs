using System.ComponentModel.DataAnnotations;

namespace LaPizzaria.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email / Tên người dùng")]
        public string EmailOrUserName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ tôi")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Họ")]
        public string? FirstName { get; set; }

        [Display(Name = "Tên")]
        public string? LastName { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ManageAccountViewModel
    {
        [Required]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Họ")]
        public string? FirstName { get; set; }

        [Display(Name = "Tên")]
        public string? LastName { get; set; }

        [Url]
        [Display(Name = "Ảnh đại diện (URL)")]
        public string? AvatarUrl { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string? CurrentPassword { get; set; }

        [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string? ConfirmNewPassword { get; set; }
    }
}


