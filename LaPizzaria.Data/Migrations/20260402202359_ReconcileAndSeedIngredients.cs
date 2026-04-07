using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <summary>
    /// Không thể khôi phục đúng dữ liệu đã mất (không có .bak): (1) tạo dòng Ingredients
    /// cho mọi IngredientId đang được ProductIngredients tham chiếu nhưng chưa tồn tại;
    /// (2) nếu bảng Ingredients vẫn trống, seed bộ nguyên liệu mẫu cho pizza.
    /// </summary>
    public partial class ReconcileAndSeedIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Các món (ProductIngredients) vẫn trỏ tới Id nguyên liệu nhưng bảng Ingredients không còn dòng → tạo placeholder để khớp FK / tránh lỗi hiển thị.
            migrationBuilder.Sql(@"
SET IDENTITY_INSERT [dbo].[Ingredients] ON;

INSERT INTO [dbo].[Ingredients] ([Id], [Name], [Unit], [StockQuantity], [ReorderLevel], [IsActive])
SELECT DISTINCT pi.[IngredientId],
       N'Nguyên liệu (khôi phục #' + CAST(pi.[IngredientId] AS nvarchar(20)) + N')',
       N'g',
       CAST(5000 AS decimal(18,2)),
       CAST(500 AS decimal(18,2)),
       CAST(1 AS bit)
FROM [dbo].[ProductIngredients] pi
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Ingredients] i WHERE i.[Id] = pi.[IngredientId]
);

SET IDENTITY_INSERT [dbo].[Ingredients] OFF;
");

            // 2) DB mới hoàn toàn: chưa có nguyên liệu và không có mapping sản phẩm → seed danh mục cơ bản.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[Ingredients])
BEGIN
    SET IDENTITY_INSERT [dbo].[Ingredients] ON;

    INSERT INTO [dbo].[Ingredients] ([Id], [Name], [Unit], [StockQuantity], [ReorderLevel], [IsActive]) VALUES
    (1,  N'Bột làm đế pizza',     N'g',   10000, 500, 1),
    (2,  N'Phô mai Mozzarella',    N'g',   8000,  400, 1),
    (3,  N'Sốt cà chua',           N'ml',  5000,  300, 1),
    (4,  N'Xúc xích Pepperoni',    N'g',   3000,  200, 1),
    (5,  N'Thịt bò bằm',           N'g',   2500,  200, 1),
    (6,  N'Giăm bông',             N'g',   2500,  200, 1),
    (7,  N'Nấm tươi',              N'g',   2000,  150, 1),
    (8,  N'Ớt chuông',             N'g',   2000,  150, 1),
    (9,  N'Hành tây',              N'g',   2000,  150, 1),
    (10, N'Thơm (dứa)',            N'g',   1500,  100, 1),
    (11, N'Tôm sú',                N'g',   2000,  150, 1),
    (12, N'Cá ngừ',                N'g',   1500,  100, 1),
    (13, N'Rau rocket',            N'g',   1000,  100, 1),
    (14, N'Ô liu',                 N'g',   1500,  100, 1),
    (15, N'Dầu ô liu',             N'ml',  2000,  200, 1),
    (16, N'Tỏi',                   N'g',   500,   100, 1),
    (17, N'Oregano / lá thơm',     N'g',   300,   50,  1),
    (18, N'Men / chất nở (bột)',   N'g',   500,   100, 1);

    SET IDENTITY_INSERT [dbo].[Ingredients] OFF;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Dữ liệu seed / khôi phục: không tự động xóa để tránh mất dữ liệu người dùng đã sửa sau đó.
        }
    }
}
