using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoStorage.Migrations
{
    /// <inheritdoc />
    public partial class AddTagsToPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Photos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Photos");
        }
    }
}
