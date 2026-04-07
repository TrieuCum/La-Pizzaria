using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShipperIdToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'ShipperId') IS NULL
BEGIN
    ALTER TABLE [Orders] ADD [ShipperId] nvarchar(450) NULL;
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_ShipperId' AND object_id = OBJECT_ID('Orders'))
BEGIN
    CREATE INDEX [IX_Orders_ShipperId] ON [Orders] ([ShipperId]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Orders_AspNetUsers_ShipperId')
BEGIN
    ALTER TABLE [Orders]
    ADD CONSTRAINT [FK_Orders_AspNetUsers_ShipperId]
    FOREIGN KEY ([ShipperId]) REFERENCES [AspNetUsers]([Id]) ON DELETE SET NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Orders_AspNetUsers_ShipperId')
BEGIN
    ALTER TABLE [Orders] DROP CONSTRAINT [FK_Orders_AspNetUsers_ShipperId];
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Orders_ShipperId' AND object_id = OBJECT_ID('Orders'))
BEGIN
    DROP INDEX [IX_Orders_ShipperId] ON [Orders];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'ShipperId') IS NOT NULL
BEGIN
    ALTER TABLE [Orders] DROP COLUMN [ShipperId];
END
");
        }
    }
}
