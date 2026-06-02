using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLotAndBookingStatusReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LotStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotStatuses", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "BookingStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 0, "Активное" },
                    { 1, "Отменено" },
                    { 2, "Выдано" }
                });

            migrationBuilder.InsertData(
                table: "LotStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 0, "Активный" },
                    { 1, "Распродан" },
                    { 2, "Истек" },
                    { 3, "Отменен" }
                });

            migrationBuilder.Sql("""
                UPDATE Bookings
                SET
                    Status = 1,
                    CancelledAt = COALESCE(CancelledAt, ReservedAt)
                WHERE Status = 3;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BookingStatuses_Status",
                table: "Bookings",
                column: "Status",
                principalTable: "BookingStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodLots_LotStatuses_Status",
                table: "FoodLots",
                column: "Status",
                principalTable: "LotStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BookingStatuses_Status",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_FoodLots_LotStatuses_Status",
                table: "FoodLots");

            migrationBuilder.DropTable(
                name: "BookingStatuses");

            migrationBuilder.DropTable(
                name: "LotStatuses");
        }
    }
}
