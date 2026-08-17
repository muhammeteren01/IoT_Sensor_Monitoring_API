using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTSensorMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssignDeviceModelsToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_device_models_manufacturer_model_number",
                table: "device_models");

            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "device_models",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE device_models
                SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1)
                WHERE company_id IS NULL;
                """);

            migrationBuilder.Sql("DELETE FROM device_models WHERE company_id IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "company_id",
                table: "device_models",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_models_company_id",
                table: "device_models",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_device_models_company_id_manufacturer_model_number",
                table: "device_models",
                columns: new[] { "company_id", "manufacturer", "model_number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_device_models_companies_company_id",
                table: "device_models",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_device_models_companies_company_id",
                table: "device_models");

            migrationBuilder.DropIndex(
                name: "IX_device_models_company_id",
                table: "device_models");

            migrationBuilder.DropIndex(
                name: "IX_device_models_company_id_manufacturer_model_number",
                table: "device_models");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "device_models");

            migrationBuilder.CreateIndex(
                name: "IX_device_models_manufacturer_model_number",
                table: "device_models",
                columns: new[] { "manufacturer", "model_number" },
                unique: true);
        }
    }
}
