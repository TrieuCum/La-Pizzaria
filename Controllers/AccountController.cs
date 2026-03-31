using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using LaPizzaria.Models;
using LaPizzaria.ViewModels;
using System.Security.Claims;

namespace LaPizzaria.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly bool _googleLoginEnabled;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _googleLoginEnabled =
                !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) &&
                !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]);
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            // Đã đăng nhập (cookie còn) → chuyển đúng trang theo role
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && await _userManager.IsInRoleAsync(user, "Shipper"))
                    return RedirectToAction("Index", "Shipper");
                return RedirectToAction("Index", "Home");
            }
            ViewBag.GoogleLoginEnabled = _googleLoginEnabled;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.GoogleLoginEnabled = _googleLoginEnabled;
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.EmailOrUserName, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(model.EmailOrUserName) ?? await _userManager.FindByEmailAsync(model.EmailOrUserName);
                if (user != null && await _userManager.IsInRoleAsync(user, "Shipper"))
                {
                    TempData["success"] = "Đăng nhập thành công. Chào shipper.";
                    return RedirectToAction("Index", "Shipper");
                }
                TempData["success"] = "Đăng nhập thành công. Bạn có thể bắt đầu đặt món.";
                return RedirectToAction("Index", "Home");
            }
            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản tạm thời bị khóa. Vui lòng thử lại sau.");
                ViewBag.GoogleLoginEnabled = _googleLoginEnabled;
                return View(model);
            }
            ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không đúng.");
            ViewBag.GoogleLoginEnabled = _googleLoginEnabled;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null, string mode = "login", bool popup = false)
            => StartExternalLogin(provider, returnUrl, mode, popup);

        [HttpGet]
        public IActionResult ExternalLoginStart(string provider = "Google", string? returnUrl = null, string mode = "login", bool popup = false)
            => StartExternalLogin(provider, returnUrl, mode, popup);

        private IActionResult StartExternalLogin(string provider, string? returnUrl, string mode, bool popup)
        {
            var normalizedMode = string.Equals(mode, "register", StringComparison.OrdinalIgnoreCase) ? "register" : "login";
            if (!_googleLoginEnabled || !string.Equals(provider, "Google", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Đăng nhập Google chưa được cấu hình.";
                return RedirectToAction(normalizedMode == "register" ? nameof(Register) : nameof(Login));
            }

            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl, mode = normalizedMode, popup });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null, string mode = "login", bool popup = false)
        {
            var isRegisterFlow = string.Equals(mode, "register", StringComparison.OrdinalIgnoreCase);
            if (!_googleLoginEnabled)
            {
                return HandleExternalAuthError("Đăng nhập Google chưa được cấu hình.", isRegisterFlow, popup);
            }

            if (!string.IsNullOrEmpty(remoteError))
            {
                return HandleExternalAuthError($"Đăng nhập ngoài thất bại: {remoteError}", isRegisterFlow, popup);
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return HandleExternalAuthError("Không thể tải thông tin đăng nhập Google.", isRegisterFlow, popup);
            }

            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (result.Succeeded)
            {
                var linkedUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (linkedUser != null && await _userManager.IsInRoleAsync(linkedUser, "Shipper"))
                    return RedirectToAction("Index", "Shipper");

                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return HandleExternalAuthError("Tài khoản tạm thời bị khóa.", isRegisterFlow, popup);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return HandleExternalAuthError("Google không trả về email, không thể đăng nhập.", isRegisterFlow, popup);
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
                    LastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
                    AvatarUrl = info.Principal.FindFirst("picture")?.Value
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return HandleExternalAuthError(string.Join(" ", createResult.Errors.Select(e => e.Description)), isRegisterFlow, popup);
                }

                await EnsureDefaultUserRolesAsync(user);
            }
            else
            {
                // Map additional profile data from Google for existing users (without overwriting non-empty data).
                var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
                var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);
                var picture = info.Principal.FindFirst("picture")?.Value;
                var updated = false;

                if (string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(givenName))
                {
                    user.FirstName = givenName;
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(user.LastName) && !string.IsNullOrWhiteSpace(surname))
                {
                    user.LastName = surname;
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(user.AvatarUrl) && !string.IsNullOrWhiteSpace(picture))
                {
                    user.AvatarUrl = picture;
                    updated = true;
                }

                if (updated)
                {
                    await _userManager.UpdateAsync(user);
                }
            }

            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded &&
                !addLoginResult.Errors.Any(e => e.Code == "LoginAlreadyAssociated"))
            {
                return HandleExternalAuthError("Không thể liên kết tài khoản Google.", isRegisterFlow, popup);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            if (isRegisterFlow)
            {
                TempData["success"] = "Đăng ký bằng Google thành công. Chào mừng đến LaPizzaria!";
            }

            if (popup)
            {
                var finalUrl = Url.Action("Index", "Home") ?? "/";
                return PopupResult("success", finalUrl);
            }
            if (await _userManager.IsInRoleAsync(user, "Shipper"))
                return RedirectToAction("Index", "Shipper");

            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.GoogleLoginEnabled = _googleLoginEnabled;
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.GoogleLoginEnabled = _googleLoginEnabled;
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName ?? string.Empty,
                LastName = model.LastName ?? string.Empty,
                PhoneNumber = model.PhoneNumber
            };

            var existingByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingByEmail != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
                ViewBag.GoogleLoginEnabled = _googleLoginEnabled;
                return View(model);
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await EnsureDefaultUserRolesAsync(user);
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["success"] = "Đăng ký thành công. Chào mừng đến LaPizzaria!";
                return RedirectToAction("Index", "Home");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            ViewBag.GoogleLoginEnabled = _googleLoginEnabled;
            return View(model);
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");
            var vm = new ManageAccountViewModel
            {
                UserName = user.UserName ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AvatarUrl = user.AvatarUrl ?? string.Empty,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Birthday = user.Birthday,
                Gender = user.Gender,
                Address = user.Address
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(ManageAccountViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            // Update profile fields
            user.FirstName = model.FirstName ?? string.Empty;
            user.LastName = model.LastName ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName != user.UserName)
            {
                var setName = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setName.Succeeded)
                {
                    foreach (var e in setName.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View(model);
                }
            }
            user.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl;
            user.PhoneNumber = model.PhoneNumber;
            user.Birthday = model.Birthday;
            user.Gender = model.Gender;
            user.Address = model.Address;

            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                foreach (var e in update.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            // Change password if provided
            if (!string.IsNullOrWhiteSpace(model.CurrentPassword) && !string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!result.Succeeded)
                {
                    foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                    return View(model);
                }
            }

            TempData["success"] = "Cập nhật tài khoản thành công.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Login");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        private async Task EnsureDefaultUserRolesAsync(ApplicationUser user)
        {
            if (!await _userManager.IsInRoleAsync(user, "User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
            }

            if (!await _userManager.IsInRoleAsync(user, "Customer"))
            {
                await _userManager.AddToRoleAsync(user, "Customer");
            }
        }

        private IActionResult HandleExternalAuthError(string message, bool isRegisterFlow, bool popup)
        {
            if (popup) return PopupResult("error", null, message);
            TempData["error"] = message;
            return RedirectToAction(isRegisterFlow ? nameof(Register) : nameof(Login));
        }

        private ContentResult PopupResult(string status, string? redirectUrl = null, string? message = null)
        {
            var encodedStatus = System.Net.WebUtility.HtmlEncode(status);
            var encodedUrl = System.Net.WebUtility.HtmlEncode(redirectUrl ?? string.Empty);
            var encodedMessage = System.Net.WebUtility.HtmlEncode(message ?? string.Empty);
            var html = $@"<!doctype html>
<html>
<body>
<script>
  (function () {{
    if (window.opener && window.opener !== window) {{
      window.opener.postMessage({{
        source: 'google-auth',
        status: '{encodedStatus}',
        redirectUrl: '{encodedUrl}',
        message: '{encodedMessage}'
      }}, window.location.origin);
    }}
    window.close();
  }})();
</script>
</body>
</html>";
            return Content(html, "text/html");
        }
    }
}
