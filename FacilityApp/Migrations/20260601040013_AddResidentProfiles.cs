using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityApp.Migrations
{
    /// <inheritdoc />
    public partial class AddResidentProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "user_units",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DepositPaid",
                table: "user_units",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerName",
                table: "user_units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployerPhone",
                table: "user_units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuarantorIdNumber",
                table: "user_units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuarantorName",
                table: "user_units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuarantorPhone",
                table: "user_units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseEndDate",
                table: "user_units",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseStartDate",
                table: "user_units",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyRent",
                table: "user_units",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentalAgreementRef",
                table: "user_units",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "owner_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    KraPin = table.Column<string>(type: "text", nullable: true),
                    BankName = table.Column<string>(type: "text", nullable: true),
                    BankAccountNumber = table.Column<string>(type: "text", nullable: true),
                    BankBranch = table.Column<string>(type: "text", nullable: true),
                    LevyPaymentMethod = table.Column<string>(type: "text", nullable: true),
                    TitleDeedRef = table.Column<string>(type: "text", nullable: true),
                    IsAbsenteeOwner = table.Column<bool>(type: "boolean", nullable: false),
                    ManagingAgentName = table.Column<string>(type: "text", nullable: true),
                    ManagingAgentContact = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_owner_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_owner_profiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_owner_profiles_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resident_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    NationalId = table.Column<string>(type: "text", nullable: true),
                    PassportNumber = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    PhysicalAddress = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "text", nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "text", nullable: true),
                    NextOfKinName = table.Column<string>(type: "text", nullable: true),
                    NextOfKinPhone = table.Column<string>(type: "text", nullable: true),
                    NextOfKinRelationship = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resident_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_resident_profiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resident_profiles_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_owner_profiles_TenantId_UserId",
                table: "owner_profiles",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_owner_profiles_UserId",
                table: "owner_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resident_profiles_TenantId_NationalId",
                table: "resident_profiles",
                columns: new[] { "TenantId", "NationalId" },
                unique: true,
                filter: "\"NationalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_resident_profiles_TenantId_UserId",
                table: "resident_profiles",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resident_profiles_UserId",
                table: "resident_profiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "owner_profiles");

            migrationBuilder.DropTable(
                name: "resident_profiles");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "DepositPaid",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "EmployerName",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "EmployerPhone",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "GuarantorIdNumber",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "GuarantorName",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "GuarantorPhone",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "LeaseEndDate",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "LeaseStartDate",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "MonthlyRent",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "RentalAgreementRef",
                table: "user_units");
        }
    }
}
