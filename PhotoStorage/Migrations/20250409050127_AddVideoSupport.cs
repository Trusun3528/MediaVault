using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoStorage.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "Photos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "Photos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailFileName",
                table: "Photos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ThumbnailFileName",
                table: "Photos");
        }
    }
}
