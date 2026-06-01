using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanatoriumIS.Migrations
{
    /// <inheritdoc />
    public partial class PriceRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoomPrices_Capacity_Category",
                table: "RoomPrices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RoomPrices_Capacity_Category",
                table: "RoomPrices",
                columns: new[] { "Capacity", "Category" },
                unique: true);
        }
    }
}
