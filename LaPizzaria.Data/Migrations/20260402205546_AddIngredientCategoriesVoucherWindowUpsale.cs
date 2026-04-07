using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientCategoriesVoucherWindowUpsale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UpsaleRequiresSlowSeller",
                table: "Vouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ValidDaysOfWeek",
                table: "Vouchers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WindowTimeEndMinute",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WindowTimeStartMinute",
                table: "Vouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSlowSeller",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Ingredients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSecondary",
                table: "Ingredients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "IngredientCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngredientCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IngredientCategories_IngredientCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "IngredientCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_CategoryId",
                table: "Ingredients",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IngredientCategories_ParentId",
                table: "IngredientCategories",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ingredients_IngredientCategories_CategoryId",
                table: "Ingredients",
                column: "CategoryId",
                principalTable: "IngredientCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[IngredientCategories])
BEGIN
    SET IDENTITY_INSERT [dbo].[IngredientCategories] ON;
    INSERT INTO [dbo].[IngredientCategories] ([Id], [Name], [ParentId], [SortOrder]) VALUES
    (1, N'Nấm', NULL, 1),
    (2, N'Rau củ', NULL, 2),
    (3, N'Bò', NULL, 3),
    (4, N'Hải sản', NULL, 4),
    (5, N'Đồ uống / Sốt', NULL, 5),
    (6, N'Khác', NULL, 6);
    SET IDENTITY_INSERT [dbo].[IngredientCategories] OFF;
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
                name: "UpsaleRequiresSlowSeller",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ValidDaysOfWeek",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "WindowTimeEndMinute",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "WindowTimeStartMinute",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "IsSlowSeller",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Ingredients");

            migrationBuilder.DropColumn(
                name: "IsSecondary",
                table: "Ingredients");
        }
    }
}
