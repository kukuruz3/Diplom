using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanatoriumIS.Migrations
{
    /// <inheritdoc />
    public partial class AddProcedureStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ProcedureAssignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "ProcedureAssignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "ProcedureAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "ProcedureAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "ProcedureAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletedBy",
                table: "ProcedureAssignments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "ProcedureAssignments");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "ProcedureAssignments");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "ProcedureAssignments");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ProcedureAssignments");

            migrationBuilder.DropColumn(
                name: "CompletedBy",
                table: "ProcedureAssignments");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ProcedureAssignments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
