-- ============================================================
-- Thêm role Shipper (nếu chưa có) và gán user thành Shipper
-- ============================================================
-- Cách 1: Gán user ĐÃ TỒN TẠI (đã đăng ký) thành Shipper
--         Thay 'email@example.com' bằng email user bạn muốn làm shipper.
-- Cách 2: Tạo user mới qua app (Đăng ký), sau đó chạy phần INSERT AspNetUserRoles bên dưới với email đó.
-- ============================================================

USE [LaPizzariaDb]
GO

-- 1) Đảm bảo role Shipper tồn tại
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = N'Shipper')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), N'Shipper', N'SHIPPER', NULL);
END
GO

-- 2) Gán user có email sau thành Shipper (chỉ chạy 1 lần, đổi email cho đúng)
-- Thay N'shipper@lapizzaria.com' bằng email tài khoản bạn muốn đăng nhập trang Shipper
DECLARE @UserId NVARCHAR(450);
DECLARE @RoleId NVARCHAR(450);

SELECT @UserId = Id FROM AspNetUsers WHERE Email = N'shipper@lapizzaria.com' OR UserName = N'shipper@lapizzaria.com';
SELECT @RoleId = Id FROM AspNetRoles WHERE Name = N'Shipper';

IF @UserId IS NOT NULL AND @RoleId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId AND RoleId = @RoleId)
        INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId);
END
-- Nếu @UserId NULL: chưa có user với email đó → hãy đăng ký trước (hoặc dùng seed trong app để tạo shipper@lapizzaria.com)
GO
