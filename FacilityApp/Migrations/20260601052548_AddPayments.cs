using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MpesaConsumerKey",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MpesaConsumerSecret",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MpesaEnabled",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MpesaPasskey",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MpesaSandbox",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MpesaShortCode",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<string>(type: "text", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    MpesaReceiptNo = table.Column<string>(type: "text", nullable: true),
                    CheckoutRequestId = table.Column<string>(type: "text", nullable: true),
                    MerchantRequestId = table.Column<string>(type: "text", nullable: true),
                    ResultDescription = table.Column<string>(type: "text", nullable: true),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RecordedById = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payments_AspNetUsers_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payments_AspNetUsers_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payments_units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payments_RecordedById",
                table: "payments",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_payments_ResidentId",
                table: "payments",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_TenantId_CheckoutRequestId",
                table: "payments",
                columns: new[] { "TenantId", "CheckoutRequestId" },
                filter: "\"CheckoutRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payments_UnitId",
                table: "payments",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropColumn(
                name: "MpesaConsumerKey",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "MpesaConsumerSecret",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "MpesaEnabled",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "MpesaPasskey",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "MpesaSandbox",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "MpesaShortCode",
                table: "tenants");
        }
    }
}
