using Microsoft.EntityFrameworkCore;
using LaPizzaria.Data;
using LaPizzaria.Models;
using LaPizzaria.Hubs;
using LaPizzaria.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// DI for application services
builder.Services.AddScoped<IComboService, ComboService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IQrService, QrService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
// builder.Services.AddHostedService<VoucherCleanupService>();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
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
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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

// Đảm bảo các role Admin, Staff, Shipper tồn tại (seed nếu chưa có)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in new[] { "Admin", "Staff", "Shipper", "Customer" })
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


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapHub<OrderingHub>("/hub/ordering");

app.Run();