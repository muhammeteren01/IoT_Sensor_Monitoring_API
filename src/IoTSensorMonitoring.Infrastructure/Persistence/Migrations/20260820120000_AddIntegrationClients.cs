using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTSensorMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "integration_clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    client_secret_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_clients", x => x.id);
                    table.ForeignKey(
                        name: "FK_integration_clients_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_integration_clients_client_id",
                table: "integration_clients",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_clients_company_id",
                table: "integration_clients",
                column: "company_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_clients");
        }
    }
}
