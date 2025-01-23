using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace De.Hsfl.LoomChat.Chat.Migrations
{
    /// <inheritdoc />
    public partial class channel_is_dm_channel_added : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDmChannel",
                table: "Channels",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDmChannel",
                table: "Channels");
        }
    }
}
