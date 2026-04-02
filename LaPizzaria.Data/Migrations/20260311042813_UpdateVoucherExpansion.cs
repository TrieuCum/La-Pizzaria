using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVoucherExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Vouchers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinOrderValue",
                table: "Vouchers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TargetUserId",
                table: "Vouchers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoucherType",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_TargetUserId",
                table: "Vouchers",
                column: "TargetUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_AspNetUsers_TargetUserId",
                table: "Vouchers",
                column: "TargetUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_AspNetUsers_TargetUserId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_TargetUserId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "MinOrderValue",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "VoucherType",
                table: "Vouchers");
        }
    }
}
