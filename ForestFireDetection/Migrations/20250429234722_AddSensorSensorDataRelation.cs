using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForestFireDetection.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorSensorDataRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SensorState",
                table: "Sensors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(6)",
                oldMaxLength: 6);

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_SensorId",
                table: "SensorData",
                column: "SensorId");

            migrationBuilder.AddForeignKey(
                name: "FK_SensorData_Sensors_SensorId",
                table: "SensorData",
                column: "SensorId",
                principalTable: "Sensors",
                principalColumn: "SensorId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SensorData_Sensors_SensorId",
                table: "SensorData");

            migrationBuilder.DropIndex(
                name: "IX_SensorData_SensorId",
                table: "SensorData");

            migrationBuilder.AlterColumn<string>(
                name: "SensorState",
                table: "Sensors",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
