using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class AddLotComponents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LotComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Details = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Composition = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotComponents", x => x.Id);
                    table.CheckConstraint("CK_LotComponents_Quantity_Positive", "Quantity > 0");
                    table.ForeignKey(
                        name: "FK_LotComponents_FoodLots_LotId",
                        column: x => x.LotId,
                        principalTable: "FoodLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO LotComponents (Id, LotId, Name, Quantity, Details, Composition, SortOrder)
                SELECT
                    lower(hex(randomblob(4))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(2))) || '-' ||
                    lower(hex(randomblob(6))),
                    FoodLots.Id,
                    'Состав набора',
                    1,
                    NULL,
                    FoodLots.Composition,
                    0
                FROM FoodLots
                WHERE trim(FoodLots.Composition) <> '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_LotComponents_LotId",
                table: "LotComponents",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_LotComponents_LotId_SortOrder",
                table: "LotComponents",
                columns: new[] { "LotId", "SortOrder" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LotComponents");
        }
    }
}
