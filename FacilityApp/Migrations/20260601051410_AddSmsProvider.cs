using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSmsProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SmsApiUrl",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmsProvider",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SmsApiUrl",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "SmsProvider",
                table: "tenants");
        }
    }
}
