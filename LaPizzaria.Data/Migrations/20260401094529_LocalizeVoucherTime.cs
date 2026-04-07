using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocalizeVoucherTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Vouchers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "StartsAtUtc",
                table: "Vouchers",
                newName: "StartsAt");

            migrationBuilder.RenameColumn(
                name: "ExpiresAtUtc",
                table: "Vouchers",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Vouchers",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Vouchers",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "StartsAt",
                table: "Vouchers",
                newName: "StartsAtUtc");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "Vouchers",
                newName: "ExpiresAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Vouchers",
                newName: "CreatedAtUtc");
        }
    }
}
