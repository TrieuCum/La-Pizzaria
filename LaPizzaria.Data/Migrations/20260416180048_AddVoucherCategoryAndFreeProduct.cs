using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherCategoryAndFreeProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppliesTo",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FreeProductId",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFreeByVoucher",
                table: "OrderDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_FreeProductId",
                table: "Vouchers",
                column: "FreeProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vouchers_Products_FreeProductId",
                table: "Vouchers",
                column: "FreeProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vouchers_Products_FreeProductId",
                table: "Vouchers");

            migrationBuilder.DropIndex(
                name: "IX_Vouchers_FreeProductId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "AppliesTo",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "FreeProductId",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "IsFreeByVoucher",
                table: "OrderDetails");
        }
    }
}
