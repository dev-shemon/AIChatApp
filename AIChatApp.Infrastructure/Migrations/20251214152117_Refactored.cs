using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIChatApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Refactored : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "ChatMessages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
