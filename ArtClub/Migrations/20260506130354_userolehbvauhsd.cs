using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtClub.Migrations
{
    /// <inheritdoc />
    public partial class userolehbvauhsd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMembershipActive",
                table: "Users",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMembershipActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Payments");
        }
    }
}
