using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddRideHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Rides",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PickupLongitude",
                table: "Rides",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,7)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PickupLatitude",
                table: "Rides",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,7)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DropoffLongitude",
                table: "Rides",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,7)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DropoffLatitude",
                table: "Rides",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,7)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Distance",
                table: "Rides",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.CreateTable(
                name: "RideHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PassengerId = table.Column<int>(type: "integer", nullable: false),
                    DriverId = table.Column<int>(type: "integer", nullable: true),
                    PickupLocation = table.Column<string>(type: "text", nullable: false),
                    PickupLatitude = table.Column<decimal>(type: "numeric", nullable: false),
                    PickupLongitude = table.Column<decimal>(type: "numeric", nullable: false),
                    DropoffLocation = table.Column<string>(type: "text", nullable: false),
                    DropoffLatitude = table.Column<decimal>(type: "numeric", nullable: false),
                    DropoffLongitude = table.Column<decimal>(type: "numeric", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Distance = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideHistories", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RideHistories");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "Rides",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PickupLongitude",
                table: "Rides",
                type: "numeric(10,7)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PickupLatitude",
                table: "Rides",
                type: "numeric(10,7)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "DropoffLongitude",
                table: "Rides",
                type: "numeric(10,7)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "DropoffLatitude",
                table: "Rides",
                type: "numeric(10,7)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Distance",
                table: "Rides",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
