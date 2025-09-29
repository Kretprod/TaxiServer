using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverDetails",
                columns: table => new
                {
                    DriverId = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    CarNumber = table.Column<string>(type: "text", nullable: false),
                    DriverLicenseNumber = table.Column<string>(type: "text", nullable: false),
                    PassportPhotoUrl = table.Column<string>(type: "text", nullable: false),
                    CarPhotoUrl = table.Column<string>(type: "text", nullable: false),
                    DriverLicensePhotoUrl = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverDetails", x => x.DriverId);
                    table.ForeignKey(
                        name: "FK_DriverDetails_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverDetails");
        }
    }
}
