using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForestFireDetection.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorHourlySummaryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SensorHourlySummary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SensorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false),
                    AvgTemperature = table.Column<double>(type: "float", nullable: false),
                    AvgHumidity = table.Column<double>(type: "float", nullable: false),
                    AvgSmoke = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorHourlySummary", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensorHourlySummary");
        }
    }
}
