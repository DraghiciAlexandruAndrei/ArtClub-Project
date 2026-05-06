using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtClub.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceTypeAndQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantityAvailable",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Resources",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityAvailable",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Resources");
        }
    }
}
