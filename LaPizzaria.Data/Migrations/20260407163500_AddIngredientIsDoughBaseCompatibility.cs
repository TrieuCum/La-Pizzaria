using LaPizzaria.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LaPizzaria.Data.Migrations
{
	[DbContext(typeof(ApplicationDbContext))]
	[Migration("20260407163500_AddIngredientIsDoughBaseCompatibility")]
	public partial class AddIngredientIsDoughBaseCompatibility : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Ingredients', 'IsDoughBase') IS NULL
BEGIN
    ALTER TABLE [dbo].[Ingredients]
    ADD [IsDoughBase] bit NOT NULL
        CONSTRAINT [DF_Ingredients_IsDoughBase] DEFAULT(CAST(0 AS bit));
END
");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Ingredients', 'IsDoughBase') IS NOT NULL
BEGIN
    DECLARE @df sysname;
    SELECT @df = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Ingredients]')
      AND c.name = N'IsDoughBase';

    IF @df IS NOT NULL
        EXEC('ALTER TABLE [dbo].[Ingredients] DROP CONSTRAINT [' + @df + ']');

    ALTER TABLE [dbo].[Ingredients] DROP COLUMN [IsDoughBase];
END
");
		}
	}
}
