using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientPricingAndDoughFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDoughBase",
                table: "Ingredients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "Ingredients",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDoughBase",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "Ingredients");
        }
    }
}
