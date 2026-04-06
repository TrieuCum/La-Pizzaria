using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientCategoriesProfitAndVoucherPromo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make migration idempotent for environments where some columns/tables were added manually.
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Vouchers', 'TimeEndMinute') IS NULL
    ALTER TABLE [dbo].[Vouchers] ADD [TimeEndMinute] int NULL;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Vouchers', 'TimeStartMinute') IS NULL
    ALTER TABLE [dbo].[Vouchers] ADD [TimeStartMinute] int NULL;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Vouchers', 'UpsaleRequiresSlowSeller') IS NULL
    ALTER TABLE [dbo].[Vouchers] ADD [UpsaleRequiresSlowSeller] bit NOT NULL CONSTRAINT [DF_Vouchers_UpsaleRequiresSlowSeller] DEFAULT(CAST(0 AS bit));
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Vouchers', 'ValidDaysOfWeek') IS NULL
    ALTER TABLE [dbo].[Vouchers] ADD [ValidDaysOfWeek] nvarchar(max) NULL;
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Products', 'IsSlowSeller') IS NULL
    ALTER TABLE [dbo].[Products] ADD [IsSlowSeller] bit NOT NULL CONSTRAINT [DF_Products_IsSlowSeller] DEFAULT(CAST(0 AS bit));
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Products', 'ProfitMarginPercent') IS NULL
    ALTER TABLE [dbo].[Products] ADD [ProfitMarginPercent] decimal(18,2) NOT NULL CONSTRAINT [DF_Products_ProfitMarginPercent] DEFAULT(30.0);
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Ingredients', 'CategoryId') IS NULL
    ALTER TABLE [dbo].[Ingredients] ADD [CategoryId] int NULL;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[IngredientCategories]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[IngredientCategories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_IngredientCategories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IngredientCategories_IngredientCategories_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [dbo].[IngredientCategories] ([Id]) ON DELETE NO ACTION
    );
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Ingredients_CategoryId' AND object_id = OBJECT_ID(N'[dbo].[Ingredients]'))
    CREATE INDEX [IX_Ingredients_CategoryId] ON [dbo].[Ingredients] ([CategoryId]);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[IngredientCategories]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IngredientCategories_ParentId' AND object_id = OBJECT_ID(N'[dbo].[IngredientCategories]'))
    CREATE INDEX [IX_IngredientCategories_ParentId] ON [dbo].[IngredientCategories] ([ParentId]);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[IngredientCategories]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Ingredients_IngredientCategories_CategoryId')
    ALTER TABLE [dbo].[Ingredients]
    ADD CONSTRAINT [FK_Ingredients_IngredientCategories_CategoryId]
    FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[IngredientCategories] ([Id]) ON DELETE SET NULL;
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM IngredientCategories)
BEGIN
  INSERT INTO IngredientCategories (Name, ParentId, SortOrder) VALUES
  (N'Nấm', NULL, 1),
  (N'Rau củ', NULL, 2),
  (N'Bò', NULL, 3),
  (N'Hải sản', NULL, 4),
  (N'Đồ uống', NULL, 5),
  (N'Phụ liệu', NULL, 6);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ingredients_IngredientCategories_CategoryId",
                table: "Ingredients");

            migrationBuilder.DropTable(
                name: "IngredientCategories");

            migrationBuilder.DropIndex(
                name: "IX_Ingredients_CategoryId",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "TimeEndMinute",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "TimeStartMinute",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "UpsaleRequiresSlowSeller",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ValidDaysOfWeek",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "IsSlowSeller",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProfitMarginPercent",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Ingredients");
        }
    }
}
