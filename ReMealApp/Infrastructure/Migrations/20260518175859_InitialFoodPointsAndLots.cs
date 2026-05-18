using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReMeal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialFoodPointsAndLots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoodPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoodLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FoodPointId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Composition = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    TotalQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PickupDeadline = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodLots_FoodPoints_FoodPointId",
                        column: x => x.FoodPointId,
                        principalTable: "FoodPoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoodLots_FoodPointId",
                table: "FoodLots",
                column: "FoodPointId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLots_PickupDeadline",
                table: "FoodLots",
                column: "PickupDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLots_Status",
                table: "FoodLots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FoodPoints_OwnerId",
                table: "FoodPoints",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoodLots");

            migrationBuilder.DropTable(
                name: "FoodPoints");
        }
    }
}
