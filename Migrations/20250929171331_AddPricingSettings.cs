using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PricingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BasePrice = table.Column<decimal>(type: "numeric", nullable: false),
                    PricePerKm = table.Column<decimal>(type: "numeric", nullable: false),
                    NightMultiplier = table.Column<decimal>(type: "numeric", nullable: false),
                    BadWeatherMultiplier = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PricingSettings",
                columns: new[] { "Id", "BadWeatherMultiplier", "BasePrice", "NightMultiplier", "PricePerKm" },
                values: new object[] { 1, 1.3m, 50m, 1.2m, 20m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PricingSettings");
        }
    }
}
