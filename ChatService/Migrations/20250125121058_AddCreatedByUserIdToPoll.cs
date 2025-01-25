using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace De.Hsfl.LoomChat.Chat.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUserIdToPoll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Polls",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Polls");
        }
    }
}
