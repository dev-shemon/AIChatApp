using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIChatApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MessageContent",
                table: "ChatMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentType",
                table: "ChatMessages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "ChatMessages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentType",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<string>(
                name: "MessageContent",
                table: "ChatMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }
    }
}
