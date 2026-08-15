using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IoTSensorMonitoring.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityFloorCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "floor_count",
                table: "facilities",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "floor_count",
                table: "facilities");
        }
    }
}
