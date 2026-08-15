using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTSensorMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "device_models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    model_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    supported_metrics = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    calibration_period_days = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_models", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "facilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facilities", x => x.id);
                    table.ForeignKey(
                        name: "FK_facilities_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    facility_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    floor_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zones", x => x.id);
                    table.ForeignKey(
                        name: "FK_zones_facilities_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facilities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sensors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mac_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    firmware_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    last_calibration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensors", x => x.id);
                    table.ForeignKey(
                        name: "FK_sensors_device_models_device_model_id",
                        column: x => x.device_model_id,
                        principalTable: "device_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sensors_zones_zone_id",
                        column: x => x.zone_id,
                        principalTable: "zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alert_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    comparison_operator = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    threshold = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_rules", x => x.id);
                    table.ForeignKey(
                        name: "FK_alert_rules_sensors_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_maintenance_logs_sensors_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sensor_measurements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    humidity = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    pressure = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    battery_level = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    signal_strength = table.Column<int>(type: "integer", nullable: true),
                    measurement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor_measurements", x => x.id);
                    table.ForeignKey(
                        name: "FK_sensor_measurements_sensors_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alert_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_value = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    triggered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_alert_history_alert_rules_alert_rule_id",
                        column: x => x.alert_rule_id,
                        principalTable: "alert_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_alert_history_sensors_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_history_alert_rule_id",
                table: "alert_history",
                column: "alert_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_history_is_resolved",
                table: "alert_history",
                column: "is_resolved");

            migrationBuilder.CreateIndex(
                name: "IX_alert_history_sensor_id_triggered_at",
                table: "alert_history",
                columns: new[] { "sensor_id", "triggered_at" });

            migrationBuilder.CreateIndex(
                name: "IX_alert_rules_sensor_id",
                table: "alert_rules",
                column: "sensor_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_models_manufacturer_model_number",
                table: "device_models",
                columns: new[] { "manufacturer", "model_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_facilities_company_id",
                table: "facilities",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_logs_next_due_date",
                table: "maintenance_logs",
                column: "next_due_date");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_logs_sensor_id",
                table: "maintenance_logs",
                column: "sensor_id");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_measurements_sensor_id_measurement_date",
                table: "sensor_measurements",
                columns: new[] { "sensor_id", "measurement_date" });

            migrationBuilder.CreateIndex(
                name: "IX_sensors_device_model_id",
                table: "sensors",
                column: "device_model_id");

            migrationBuilder.CreateIndex(
                name: "IX_sensors_mac_address",
                table: "sensors",
                column: "mac_address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sensors_status",
                table: "sensors",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_sensors_zone_id",
                table: "sensors",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "IX_zones_facility_id",
                table: "zones",
                column: "facility_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_history");

            migrationBuilder.DropTable(
                name: "maintenance_logs");

            migrationBuilder.DropTable(
                name: "sensor_measurements");

            migrationBuilder.DropTable(
                name: "alert_rules");

            migrationBuilder.DropTable(
                name: "sensors");

            migrationBuilder.DropTable(
                name: "device_models");

            migrationBuilder.DropTable(
                name: "zones");

            migrationBuilder.DropTable(
                name: "facilities");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
