using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityApp.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleOwnerCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "vehicles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "OwnerCategory",
                table: "vehicles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "vehicles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerCategory",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "vehicles");

            migrationBuilder.AlterColumn<string>(
                name: "OwnerId",
                table: "vehicles",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
