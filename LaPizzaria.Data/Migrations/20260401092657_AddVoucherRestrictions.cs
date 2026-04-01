using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherRestrictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "Vouchers",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "Vouchers",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAtUtc",
                table: "Vouchers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetProductId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidDaysOfWeek",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_TargetProductId",
                table: "Vouchers",
                column: "TargetProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Products_TargetProductId",
                table: "Vouchers",
                column: "TargetProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Products_TargetProductId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_TargetProductId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "StartsAtUtc",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "TargetProductId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ValidDaysOfWeek",
                table: "Vouchers");
        }
    }
}
