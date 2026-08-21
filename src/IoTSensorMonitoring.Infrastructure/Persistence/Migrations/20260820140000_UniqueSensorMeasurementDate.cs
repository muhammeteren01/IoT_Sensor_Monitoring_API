using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTSensorMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueSensorMeasurementDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sensor_measurements_sensor_id_measurement_date",
                table: "sensor_measurements");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_measurements_sensor_id_measurement_date",
                table: "sensor_measurements",
                columns: new[] { "sensor_id", "measurement_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sensor_measurements_sensor_id_measurement_date",
                table: "sensor_measurements");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_measurements_sensor_id_measurement_date",
                table: "sensor_measurements",
                columns: new[] { "sensor_id", "measurement_date" });
        }
    }
}
