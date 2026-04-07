using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelPending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'AssignedAt') IS NULL
    ALTER TABLE [Orders] ADD [AssignedAt] datetime2 NULL;

IF COL_LENGTH('Orders', 'DeliveredAt') IS NULL
    ALTER TABLE [Orders] ADD [DeliveredAt] datetime2 NULL;

IF COL_LENGTH('Orders', 'DeliveryStatus') IS NULL
    ALTER TABLE [Orders] ADD [DeliveryStatus] nvarchar(max) NULL;

IF COL_LENGTH('Orders', 'ShipperLatitude') IS NULL
    ALTER TABLE [Orders] ADD [ShipperLatitude] float NULL;

IF COL_LENGTH('Orders', 'ShipperLocationIp') IS NULL
    ALTER TABLE [Orders] ADD [ShipperLocationIp] nvarchar(max) NULL;

IF COL_LENGTH('Orders', 'ShipperLocationUpdatedAt') IS NULL
    ALTER TABLE [Orders] ADD [ShipperLocationUpdatedAt] datetime2 NULL;

IF COL_LENGTH('Orders', 'ShipperLongitude') IS NULL
    ALTER TABLE [Orders] ADD [ShipperLongitude] float NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Orders', 'AssignedAt') IS NOT NULL
    ALTER TABLE [Orders] DROP COLUMN [AssignedAt];

IF COL_LENGTH('Orders', 'DeliveredAt') IS NOT NULL
    ALTER TABLE [Orders] DROP COLUMN [DeliveredAt];

IF COL_LENGTH('Orders', 'DeliveryStatus') IS NOT NULL
    ALTER TABLE [Orders] DROP COLUMN [DeliveryStatus];

IF COL_LENGTH('Orders', 'ShipperLatitude') IS NOT NULL
    ALTER TABLE [Orders] DROP COLUMN [ShipperLatitude];

IF COL_LENGTH('Orders', 'ShipperLocationIp') IS NOT NULL
    ALTER TABLE [Orders] DROP COLUMN [ShipperLocationIp];

IF COL_LENGTH('Orders', 'ShipperLocationUpdatedAt') IS NOT NULL
    ALTER TABLE [Orders] DROP COLUMN [ShipperLocationUpdatedAt];

IF COL_LENGTH('Orders', 'ShipperLongitude') IS NOT NULL
    ALTER TABLE [Orders] DROP COLUMN [ShipperLongitude];
");
        }
    }
}
