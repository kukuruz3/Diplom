using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanatoriumIS.Migrations
{
    /// <inheritdoc />
    public partial class AddPassportLastFourToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PassportLastFour",
                table: "Clients",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PassportLastFour",
                table: "Clients");
        }
    }
}
