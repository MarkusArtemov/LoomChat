using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace De.Hsfl.LoomChat.File.Migrations
{
    /// <inheritdoc />
    public partial class DeltaMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Documents");

            migrationBuilder.AddColumn<int>(
                name: "BaseVersionId",
                table: "DocumentVersions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFull",
                table: "DocumentVersions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FileType",
                table: "Documents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseVersionId",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "IsFull",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "FileType",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Documents",
                type: "text",
                nullable: true);
        }
    }
}
