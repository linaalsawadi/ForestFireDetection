﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ForestFireDetection.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSensorHourlySummaryStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AvgFireScore",
                table: "SensorHourlySummary",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "SensorHourlySummary",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "SensorHourlySummary",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgFireScore",
                table: "SensorHourlySummary");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "SensorHourlySummary");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "SensorHourlySummary");
        }
    }
}
