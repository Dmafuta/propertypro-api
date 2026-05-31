using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitsAndMeters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Status column first, then migrate IsOccupied data, then drop IsOccupied
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "units",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                @"UPDATE units SET ""Status"" = 1 WHERE ""IsOccupied"" = true");

            migrationBuilder.DropColumn(
                name: "IsOccupied",
                table: "units");

            migrationBuilder.AddColumn<DateTime>(
                name: "MoveInDate",
                table: "user_units",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MoveOutDate",
                table: "user_units",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Bathrooms",
                table: "units",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Bedrooms",
                table: "units",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyLevy",
                table: "units",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "units",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParkingBays",
                table: "units",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SizeM2",
                table: "units",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitTypeId",
                table: "units",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "meters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    UtilityType = table.Column<int>(type: "integer", nullable: false),
                    MeterMode = table.Column<int>(type: "integer", nullable: false),
                    MeterNumber = table.Column<string>(type: "text", nullable: false),
                    SerialNumber = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    InstallDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreviousMeterId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplacedByMeterId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meters_meters_PreviousMeterId",
                        column: x => x.PreviousMeterId,
                        principalTable: "meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_meters_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_meters_units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "unit_types",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DefaultMonthlyLevy = table.Column<decimal>(type: "numeric", nullable: true),
                    DefaultBedrooms = table.Column<int>(type: "integer", nullable: true),
                    DefaultBathrooms = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_unit_types_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meter_alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertType = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meter_alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meter_alerts_AspNetUsers_AcknowledgedByUserId",
                        column: x => x.AcknowledgedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_meter_alerts_meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "meter_readings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadingValue = table.Column<decimal>(type: "numeric", nullable: false),
                    ReadingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadingType = table.Column<int>(type: "integer", nullable: false),
                    ReadByUserId = table.Column<string>(type: "text", nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meter_readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_meter_readings_AspNetUsers_ReadByUserId",
                        column: x => x.ReadByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_meter_readings_meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prepaid_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MeterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenCode = table.Column<string>(type: "text", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitsLoaded = table.Column<decimal>(type: "numeric", nullable: true),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LoadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PurchasedByUserId = table.Column<string>(type: "text", nullable: true),
                    VoucherReference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prepaid_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prepaid_tokens_AspNetUsers_PurchasedByUserId",
                        column: x => x.PurchasedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_prepaid_tokens_meters_MeterId",
                        column: x => x.MeterId,
                        principalTable: "meters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_units_UnitTypeId",
                table: "units",
                column: "UnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_meter_alerts_AcknowledgedByUserId",
                table: "meter_alerts",
                column: "AcknowledgedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_meter_alerts_MeterId",
                table: "meter_alerts",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_meter_readings_MeterId",
                table: "meter_readings",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_meter_readings_ReadByUserId",
                table: "meter_readings",
                column: "ReadByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_meters_PreviousMeterId",
                table: "meters",
                column: "PreviousMeterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meters_TenantId",
                table: "meters",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_meters_UnitId",
                table: "meters",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_prepaid_tokens_MeterId",
                table: "prepaid_tokens",
                column: "MeterId");

            migrationBuilder.CreateIndex(
                name: "IX_prepaid_tokens_PurchasedByUserId",
                table: "prepaid_tokens",
                column: "PurchasedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_unit_types_TenantId",
                table: "unit_types",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_units_unit_types_UnitTypeId",
                table: "units",
                column: "UnitTypeId",
                principalTable: "unit_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_units_unit_types_UnitTypeId",
                table: "units");

            migrationBuilder.DropTable(
                name: "meter_alerts");

            migrationBuilder.DropTable(
                name: "meter_readings");

            migrationBuilder.DropTable(
                name: "prepaid_tokens");

            migrationBuilder.DropTable(
                name: "unit_types");

            migrationBuilder.DropTable(
                name: "meters");

            migrationBuilder.DropIndex(
                name: "IX_units_UnitTypeId",
                table: "units");

            migrationBuilder.DropColumn(
                name: "MoveInDate",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "MoveOutDate",
                table: "user_units");

            migrationBuilder.DropColumn(
                name: "Bathrooms",
                table: "units");

            migrationBuilder.DropColumn(
                name: "Bedrooms",
                table: "units");

            migrationBuilder.DropColumn(
                name: "MonthlyLevy",
                table: "units");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "units");

            migrationBuilder.DropColumn(
                name: "ParkingBays",
                table: "units");

            migrationBuilder.DropColumn(
                name: "SizeM2",
                table: "units");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "units");

            migrationBuilder.DropColumn(
                name: "UnitTypeId",
                table: "units");

            migrationBuilder.AddColumn<bool>(
                name: "IsOccupied",
                table: "units",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
