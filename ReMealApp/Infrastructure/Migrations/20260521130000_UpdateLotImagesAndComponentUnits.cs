using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class UpdateLotImagesAndComponentUnits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "FoodLots",
                type: "TEXT",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "LotComponents",
                type: "TEXT",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "LotComponents",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "шт");

            migrationBuilder.Sql("""
                UPDATE FoodLots
                SET Composition = COALESCE((
                    SELECT group_concat(DisplayText, '; ')
                    FROM (
                        SELECT
                            Name || ' — ' || Quantity || CASE
                                WHEN trim(Unit) = '' THEN ''
                                ELSE ' ' || Unit
                            END AS DisplayText
                        FROM LotComponents
                        WHERE LotComponents.LotId = FoodLots.Id
                        ORDER BY SortOrder, Name
                    )
                ), '')
                WHERE EXISTS (
                    SELECT 1
                    FROM LotComponents
                    WHERE LotComponents.LotId = FoodLots.Id
                );
                """);

            migrationBuilder.Sql("ALTER TABLE LotComponents DROP COLUMN Details;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "FoodLots");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "LotComponents");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "LotComponents");

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "LotComponents",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);
        }
    }
}
