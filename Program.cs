using Microsoft.EntityFrameworkCore;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.Hubs;
using LaPizzaria.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// IIS / reverse proxy: nhận đúng X-Forwarded-Proto (HTTPS ở edge) để cookie / redirect nhất quán
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// IIS / publish: nếu thiếu chuỗi kết nối, UseSqlServer(null) hoặc seed DB sẽ lỗi → HTTP 500.30
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnection))
{
    throw new InvalidOperationException(
        "Thiếu ConnectionStrings:DefaultConnection. Trên IIS hãy đặt ASPNETCORE_ENVIRONMENT=Production và cấu hình chuỗi kết nối trong appsettings.Production.json hoặc biến môi trường.");
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Cookie antiforgery: cùng chính sách Secure với đăng nhập (tránh POST lỗi trên HTTP)
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// DI for application services
builder.Services.AddScoped<IComboService, ComboService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IQrService, QrService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IOrderPlacementService, OrderPlacementService>();
builder.Services.AddScoped<LaPizzaria.Services.ShipperDispatchService>();
builder.Services.AddScoped<LaPizzaria.Services.EmailNotificationService>();
// builder.Services.AddHostedService<VoucherCleanupService>();

// --- Tích hợp MoMo (sandbox / production) ---
// 1) appsettings.json → section "Momo": PartnerCode, AccessKey, SecretKey, MomoApiUrl (API create),
//    ReturnUrl (redirect GET sau thanh toán), NotifyUrl (IPN POST từ MoMo).
// 2) Configure<T> nạp các giá trị đó vào MomoOptionModel để inject vào MomoService.
// 3) AddHttpClient() cung cấp IHttpClientFactory cho MomoService.CreatePaymentAsync.
// 4) Đăng ký IMomoService với lifetime Scoped (mỗi request một instance, phù hợp controller).
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("Momo"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IMomoService, MomoService>();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        defaultConnection,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(60);
        })
    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
);

// Add Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Always + site chỉ HTTP => trình duyệt không gửi cookie đăng nhập → vẫn thấy Register/Login, đặt món báo chưa đăng nhập.
// SameAsRequest: HTTPS thì cookie Secure; HTTP thì vẫn hoạt động (phù hợp host chưa SSL).
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
});

builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ExternalScheme, options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    builder.Services
        .AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.CallbackPath = "/signin-google";
            options.AccessDeniedPath = "/Account/Login";
            options.SaveTokens = true;
            // Always show Google account chooser so users can pick any existing account.
            options.Events.OnRedirectToAuthorizationEndpoint = context =>
            {
                var separator = context.RedirectUri.Contains('?') ? "&" : "?";
                context.Response.Redirect($"{context.RedirectUri}{separator}prompt=select_account");
                return Task.CompletedTask;
            };
        });
}

var app = builder.Build();

app.UseForwardedHeaders();

// Chỉ bật HSTS + chuyển sang HTTPS khi host thật sự có SSL (xem appsettings "Hosting:EnforceHttps").
// Site chỉ HTTP (nhiều gói share host) — nếu bật ép HTTPS sẽ lỗi hoặc vòng redirect.
var enforceHttps = app.Configuration.GetValue("Hosting:EnforceHttps", defaultValue: false);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    if (enforceHttps)
    {
        app.UseHsts();
    }
}

if (!app.Environment.IsDevelopment() && enforceHttps)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

// Banner images: chỉ map khi cấu hình có đường dẫn (Development/local). Production để trống hoặc set trên Azure.
var bannerPath = builder.Configuration["BannerImagesPath"] ?? "";
if (!string.IsNullOrWhiteSpace(bannerPath) && Directory.Exists(bannerPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(bannerPath),
        RequestPath = "/banners"
    });
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Đảm bảo các role hệ thống tồn tại (seed nếu chưa có)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in new[] { "Admin", "User", "Staff", "Shipper", "Customer" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }
}

// Seed tài khoản Shipper để đăng nhập trang shipper (nếu chưa có)
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    const string shipperEmail = "shipper@lapizzaria.com";
    var shipper = await userManager.FindByEmailAsync(shipperEmail);
    if (shipper == null)
    {
        shipper = new ApplicationUser
        {
            UserName = shipperEmail,
            Email = shipperEmail,
            FirstName = "Shipper",
            LastName = "La Pizzaria",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var result = await userManager.CreateAsync(shipper, "Shipper@123");
        if (result.Succeeded && await roleManager.RoleExistsAsync("Shipper"))
            await userManager.AddToRoleAsync(shipper, "Shipper");
    }
    else if (!await userManager.IsInRoleAsync(shipper, "Shipper") && await roleManager.RoleExistsAsync("Shipper"))
    {
        await userManager.AddToRoleAsync(shipper, "Shipper");
    }
}


app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapHub<OrderingHub>("/hub/ordering");

app.Run();