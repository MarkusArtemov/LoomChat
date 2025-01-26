using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace De.Hsfl.LoomChat.Chat.Migrations
{
    /// <inheritdoc />
    public partial class DifferentMessagetypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ChatMessages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "ChatMessages",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PollId",
                table: "ChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_PollId",
                table: "ChatMessages",
                column: "PollId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Polls_PollId",
                table: "ChatMessages",
                column: "PollId",
                principalTable: "Polls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Polls_PollId",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_PollId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "PollId",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ChatMessages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
