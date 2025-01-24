using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace De.Hsfl.LoomChat.File.Migrations
{
    /// <inheritdoc />
    public partial class DocumentVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UploaderUserId",
                table: "DocumentVersions",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploaderUserId",
                table: "DocumentVersions");
        }
    }
}
