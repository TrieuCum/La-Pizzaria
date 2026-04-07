using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateCustomersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Customers]', N'U') IS NULL
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [CreatedAt] datetime2 NULL,
        [CustomerCode] nvarchar(100) NULL,
        [LoyaltyPoints] int NOT NULL,
        [Status] nvarchar(100) NULL,
        [UpdatedAt] datetime2 NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Customers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF OBJECT_ID(N'[dbo].[Customers]', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_UserId' AND object_id = OBJECT_ID(N'[dbo].[Customers]'))
BEGIN
    CREATE INDEX [IX_Customers_UserId] ON [Customers] ([UserId]);
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[Customers]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [Customers];
END;
");
        }
    }
}
