using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class TelegramUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TelegramUsers",
                table: "TelegramUsers");

            migrationBuilder.RenameTable(
                name: "TelegramUsers",
                newName: "TelegramUser");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TelegramUser",
                table: "TelegramUser",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TelegramUser",
                table: "TelegramUser");

            migrationBuilder.RenameTable(
                name: "TelegramUser",
                newName: "TelegramUsers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TelegramUsers",
                table: "TelegramUsers",
                column: "Id");
        }
    }
}
