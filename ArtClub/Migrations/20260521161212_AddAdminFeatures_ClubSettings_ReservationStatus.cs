using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArtClub.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminFeatures_ClubSettings_ReservationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsExhibitionHall",
                table: "Resources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AdminOverrideById",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdminOverride",
                table: "Reservations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OverrideCreatedAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ClubSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NonMemberReservationFeePerDay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MembershipCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EventCostPerArtPiece = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EventCostPerLocation = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PendingOverrideApprovalHours = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClubSettings");

            migrationBuilder.DropColumn(
                name: "IsExhibitionHall",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "AdminOverrideById",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "IsAdminOverride",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "OverrideCreatedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Reservations");
        }
    }
}
